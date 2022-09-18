namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

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

        //TODO: do a quick test and see if we can delete everything but still get the behavior we ewant
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

                FinalizeAnimations();

                return (generatedController, generatedMenu, generatedParameters);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private VRCExpressionsMenu FinalizeMenus()
        {
            foreach (GeneratedMenu subMenu in generatedMenus)
            {
                SaveGeneratedSubMenu(subMenu);
            }

            VRCExpressionsMenu generatedRootMenu = null;
            foreach ((VRCExpressionsMenu referenceMenu, VRCExpressionsMenu workingMenu) menuPair in workingMenus)
            {
                VRCExpressionsMenu currentGeneratedMenu =
                    ReplaceOldGeneratedAssetWithWorkingAsset(menuPair.workingMenu, menuPair.referenceMenu);
                if (generatedRootMenu == null)
                {
                    generatedRootMenu = currentGeneratedMenu;
                }
            }

            return generatedRootMenu;

            void SaveGeneratedSubMenu(GeneratedMenu subMenu)
            {
                string subMenuFolder = Path.Combine(generatedPath, "GeneratedSubMenus");
                foreach (string pathComponent in subMenu.PathComponents)
                {
                    //we create folders mainly to avoid unlikely but possible name collisions
                    //such as a path=foo_bar and path=foo/bar couple of submenus
                    subMenuFolder = Path.Combine(
                        subMenuFolder,
                        Regex.Replace(
                            pathComponent,
                            $"[{Regex.Escape(new string(Path.GetInvalidPathChars()))}]", "_"));
                }
                _ = EnsureFolderCreated(subMenuFolder);

                string subMenuFullPath = Path.Combine(
                    subMenuFolder,
                    Regex.Replace(
                        $"{subMenu.PathComponents[subMenu.PathComponents.Count - 1]}.asset",
                        $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]", "_"));
                _ = ReplaceOldGeneratedAssetWithWorkingAsset(subMenu.Menu, subMenuFullPath);
            }
        }

        private void FinalizeAnimations() { }
        //our guid rewriting is easier, since we only care about the meta file
        //as there is no cross-referencing between assets for which this method is invoked
        private T ReplaceOldGeneratedAssetWithWorkingAsset<T>(T asset, T referenceAsset) where T : UnityEngine.Object
        {
            string generatedAssetPath = Path.Combine(
                generatedPath,
                $"EZFXLayer_{Path.GetFileName(AssetDatabase.GetAssetPath(referenceAsset))}");

            return ReplaceOldGeneratedAssetWithWorkingAsset(asset, generatedAssetPath);
        }
        private static T ReplaceOldGeneratedAssetWithWorkingAsset<T>(T asset, string generatedAssetPath) where T : UnityEngine.Object
        {
            T generatedAsset = AssetDatabase.LoadAssetAtPath<T>(generatedAssetPath);
            if (generatedAsset != null)
            {
                EditorUtility.CopySerialized(asset, generatedAsset);
                //without reimporting, the name shows up a bit wrong (perhaps fixes on restart didnt check)
                //no big deal, but we'll fix it anyway
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
                nameof(path), $"Path '{path}' is not rooted in Assets.");
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
