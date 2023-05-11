namespace EZUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    using static Localization;

    internal class EZFXLayerAssetRepository : IAssetRepository
    {
        private readonly string generatedPath;

        private readonly AnimatorController referenceController;
        private readonly VRCExpressionsMenu referenceRootMenu;
        private readonly VRCExpressionParameters referenceParameters;
        private readonly List<GeneratedClip> generatedClips = new List<GeneratedClip>();
        private readonly List<GeneratedMenu> generatedMenus = new List<GeneratedMenu>();
        private readonly List<UnityEngine.Object> generatedControllerSubassets = new List<UnityEngine.Object>();

        private AnimatorController workingController;
        private readonly List<(VRCExpressionsMenu referenceMenu, VRCExpressionsMenu workingMenu)> workingMenus =
            new List<(VRCExpressionsMenu referenceMenu, VRCExpressionsMenu workingMenu)>();
        private VRCExpressionParameters workingParameters;

        public EZFXLayerAssetRepository(
            string outputPath,
            AnimatorController referenceController,
            VRCExpressionsMenu referenceMenu,
            VRCExpressionParameters referenceParameters)
        {
            generatedPath = Path.Combine(outputPath, "generated");
            this.referenceController = referenceController;
            referenceRootMenu = referenceMenu;
            this.referenceParameters = referenceParameters;
        }

        public (AnimatorController, VRCExpressionsMenu, VRCExpressionParameters) PrepareWorkingAssets()
        {
            _ = EnsureFolderCreated(generatedPath);

            workingController = new AnimatorController();
            EditorUtility.CopySerialized(referenceController, workingController);

            VRCExpressionsMenu workingRootMenu = RecordPreExistingMenu(referenceRootMenu);

            workingParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            EditorUtility.CopySerialized(referenceParameters, workingParameters);


            return
            (
                workingController,
                workingRootMenu,
                workingParameters
            );

            VRCExpressionsMenu RecordPreExistingMenu(VRCExpressionsMenu referenceMenu)
            {
                VRCExpressionsMenu workingMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                EditorUtility.CopySerialized(referenceMenu, workingMenu);
#pragma warning disable IDE0037 //simplify valuetuple names
                workingMenus.Add((referenceMenu: referenceMenu, workingMenu: workingMenu));
#pragma warning restore IDE0037

                foreach (VRCExpressionsMenu.Control control in workingMenu.controls
                    .Where(c => c.type == VRCExpressionsMenu.Control.ControlType.SubMenu))
                {
                    control.subMenu = RecordPreExistingMenu(control.subMenu);
                }

                return workingMenu;
            }
        }

        //there are two goals with regards to how we do this: performance/time and git
        //the most difficult one is the animator controller, since it has many sub assets
        //currently, we mostly give up on this
        public (AnimatorController, VRCExpressionsMenu, VRCExpressionParameters) FinalizeAssets()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                AnimatorController generatedController =
                    ReplaceOldGeneratedAssetWithWorkingAsset(workingController, referenceController);
                foreach (UnityEngine.Object subAsset in generatedControllerSubassets)
                {
                    AssetDatabase.AddObjectToAsset(subAsset, generatedController);
                }

                VRCExpressionsMenu generatedMenu = FinalizeMenus();

                VRCExpressionParameters generatedParameters =
                    ReplaceOldGeneratedAssetWithWorkingAsset(workingParameters, referenceParameters);

                FinalizeAnimations(generatedController);

                return (generatedController, generatedMenu, generatedParameters);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private void FinalizeAnimations(AnimatorController generatedController)
        {
            Dictionary<Motion, AnimatorState[]> workingClipsToGeneratedAnimatorStates = generatedController.layers
                .SelectMany(l => l.stateMachine.states.Select(cs => cs.state))
                .GroupBy(s => s.motion, (motion, states) => (motion, states))
                .Where(g => g.motion != null)
                .ToDictionary(g => g.motion, g => g.states.ToArray());
            foreach (GeneratedClip workingClip in generatedClips)
            {
                string animationFolder = EnsureFolderCreated(
                    Path.Combine(
                        generatedPath,
                        "GeneratedClips",
                        EscapeFolderName(workingClip.LayerName)));
                string animationPath = Path.Combine(animationFolder, $"{EscapeFileName(workingClip.AnimationName)}.anim");
                AnimationClip generatedClip = ReplaceOldGeneratedAssetWithWorkingAsset(workingClip.Clip, animationPath);

                IEnumerable<AnimatorState> generatedAnimatorStates = workingClipsToGeneratedAnimatorStates[workingClip.Clip];

                foreach (AnimatorState generatedAnimatorState in generatedAnimatorStates)
                {
                    generatedAnimatorState.motion = generatedClip;
                }
            }
        }

        private VRCExpressionsMenu FinalizeMenus()
        {
            (VRCExpressionsMenu referenceRootMenu, VRCExpressionsMenu workingRootMenu) = workingMenus.First();
            VRCExpressionsMenu generatedRootMenu =
                ReplaceOldGeneratedAssetWithWorkingAsset(workingRootMenu, referenceRootMenu);

            Dictionary<VRCExpressionsMenu, VRCExpressionsMenu> workingToGeneratedSubMenus =
                generatedMenus
                    .Select(m => SaveGeneratedSubMenu(m))
                    .Union(
                        workingMenus.Skip(1).Select(menu =>
                        (
                            menu.workingMenu,
                            generatedMenu: ReplaceOldGeneratedAssetWithWorkingAsset(menu.workingMenu, menu.referenceMenu)
                        )))
                    .ToDictionary(menu => menu.workingMenu, menu => menu.generatedMenu);
            workingToGeneratedSubMenus[workingRootMenu] = generatedRootMenu;

            ReplaceWorkingMenusWithGeneratedMenus(workingRootMenu);

            return generatedRootMenu;

            (VRCExpressionsMenu workingMenu, VRCExpressionsMenu generatedMenu) SaveGeneratedSubMenu(GeneratedMenu subMenu)
            {
                //we create folders mainly to avoid unlikely but possible name collisions
                //such as a path=foo_bar and path=foo/bar couple of submenus
                string subMenuFolder = subMenu.PathComponents.Aggregate(
                    Path.Combine(generatedPath, "GeneratedSubMenus"),
                    (acc, next) => Path.Combine(acc, EscapeFolderName(next)));
                _ = EnsureFolderCreated(subMenuFolder);

                string subMenuFullPath = Path.Combine(
                    subMenuFolder,
                    EscapeFileName($"{subMenu.PathComponents[subMenu.PathComponents.Count - 1]}.asset"));
                VRCExpressionsMenu workingMenu = subMenu.Menu;
                VRCExpressionsMenu generatedMenu = ReplaceOldGeneratedAssetWithWorkingAsset(subMenu.Menu, subMenuFullPath);
#pragma warning disable IDE0037 //simplify tuple name. want to make sure we get it in right order
                return (workingMenu: workingMenu, generatedMenu: generatedMenu);
#pragma warning restore IDE0037
            }

            void ReplaceWorkingMenusWithGeneratedMenus(VRCExpressionsMenu workingParentMenu)
            {
                for (int i = 0; i < workingParentMenu.controls.Count; i++)
                {
                    VRCExpressionsMenu.Control control = workingParentMenu.controls[i];
                    if (control.type != VRCExpressionsMenu.Control.ControlType.SubMenu) continue;

                    //order of this should not matter; can come before or after the swap
                    ReplaceWorkingMenusWithGeneratedMenus(control.subMenu);

                    VRCExpressionsMenu generatedSubMenu = workingToGeneratedSubMenus[control.subMenu];
                    VRCExpressionsMenu generatedParentMenu = workingToGeneratedSubMenus[workingParentMenu];
                    generatedParentMenu.controls[i].subMenu = generatedSubMenu;
                }
            }
        }

        private T ReplaceOldGeneratedAssetWithWorkingAsset<T>(T asset, T referenceAsset) where T : UnityEngine.Object
        {
            string generatedAssetPath = Path.Combine(
                generatedPath,
                $"EZFXLayer_{Path.GetFileName(AssetDatabase.GetAssetPath(referenceAsset))}");

            return ReplaceOldGeneratedAssetWithWorkingAsset(asset, generatedAssetPath);
        }
        //this particular trick maintains the asset (guid). it saves on asset importing time versus copying,
        //reduces git changes, and reduces prefab changes
        private static T ReplaceOldGeneratedAssetWithWorkingAsset<T>(T asset, string generatedAssetPath) where T : UnityEngine.Object
        {
            T generatedAsset = AssetDatabase.LoadAssetAtPath<T>(generatedAssetPath);
            if (generatedAsset != null)
            {
                Undo.RecordObject(generatedAsset, UxmlLocalizationHelper.T("Overwrite EZFXLayer generated asset"));
                EditorUtility.CopySerialized(asset, generatedAsset);
                //without reimporting, the name shows up a bit wrong (perhaps fixes on restart didnt check)
                //no big deal, but we'll fix it anyway. also didn't check if the undo renders this unnecessary; prob doesnt
                AssetDatabase.ImportAsset(generatedAssetPath);
                return generatedAsset;
            }
            else
            {
                AssetDatabase.CreateAsset(asset, generatedAssetPath);
                return asset;
            }
        }

        private static string EnsureFolderCreated(string path)
        {
            if (!Regex.IsMatch(path, @"^Assets[/\\]")) throw new ArgumentOutOfRangeException(
                nameof(path), T($"Path '{path}' is not rooted in Assets."));
            if (AssetDatabase.IsValidFolder(path)) return path;

            string[] splitPath = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string currentPath = "Assets";
            foreach (string pathComponent in splitPath.Skip(1))
            {
                //since unity uses /, avoid Path.Combine in case windows adds \
                string targetPath = $"{currentPath}/{pathComponent}";
                if (!AssetDatabase.IsValidFolder(targetPath))
                {
                    _ = AssetDatabase.CreateFolder(currentPath, pathComponent);
                }
                currentPath = targetPath;
            }
            return currentPath;
        }

        private static string EscapeFileName(string name)
            => Regex.Replace(name, $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", "_");

        private static string EscapeFolderName(string name)
            => Regex.Replace(name, $"[{Regex.Escape(new string(Path.GetInvalidPathChars()))}]", "_");

        void IAssetRepository.AnimationClipAdded(GeneratedClip clip) => generatedClips.Add(clip);
        void IAssetRepository.VRCSubMenuAdded(GeneratedMenu menu) => generatedMenus.Add(menu);
        void IAssetRepository.FXAnimatorControllerStateAdded(AnimatorState animatorState)
            => generatedControllerSubassets.Add(animatorState);
        //in testing, the subasset disappears one way or another, so dont care about this
        void IAssetRepository.FXAnimatorControllerStateRemoved(AnimatorState animatorState) { }
        void IAssetRepository.FXAnimatorStateMachineAdded(AnimatorStateMachine stateMachine)
            => generatedControllerSubassets.Add(stateMachine);
        void IAssetRepository.FXAnimatorTransitionAdded(AnimatorStateTransition transition)
            => generatedControllerSubassets.Add(transition);
    }
}
