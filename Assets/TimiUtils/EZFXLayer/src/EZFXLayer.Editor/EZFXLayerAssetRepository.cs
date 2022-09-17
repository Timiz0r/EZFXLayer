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
        private readonly VRCExpressionsMenu referenceMenu;
        private readonly VRCExpressionParameters referenceParameters;
        private readonly List<GeneratedClip> generatedClips = new List<GeneratedClip>();
        private readonly List<GeneratedMenu> generatedMenus = new List<GeneratedMenu>();
        private readonly List<UnityEngine.Object> generatedControllerSubassets = new List<UnityEngine.Object>();

        private AnimatorController workingController;
        private VRCExpressionsMenu workingMenu;
        private VRCExpressionParameters workingParameters;

        public EZFXLayerAssetRepository(
            string outputPath,
            AnimatorController referenceController,
            VRCExpressionsMenu referenceMenu,
            VRCExpressionParameters referenceParameters)
        {
            generatedPath = Path.Combine(outputPath, "generated");
            this.referenceController = referenceController;
            this.referenceMenu = referenceMenu;
            this.referenceParameters = referenceParameters;
        }

        public (AnimatorController, VRCExpressionsMenu, VRCExpressionParameters) PrepareWorkingAssets()
        {
            _ = EnsureFolderCreated(generatedPath);

            workingController = new AnimatorController();
            EditorUtility.CopySerialized(referenceController, workingController);
            workingMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            EditorUtility.CopySerialized(referenceMenu, workingMenu);
            workingParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            EditorUtility.CopySerialized(referenceParameters, workingParameters);

            return
            (
                workingController,
                workingMenu,
                workingParameters
            );
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
                    SwapOldGeneratedAssetWithWorkingAsset(workingController, referenceController);
                foreach (UnityEngine.Object subAsset in generatedControllerSubassets)
                {
                    AssetDatabase.AddObjectToAsset(subAsset, generatedController);
                }
                return (generatedController, null, null);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private void RewriteAnimations() => throw new NotImplementedException();

        //our guid rewriting is easier, since we only care about the meta file
        //as there is no cross-referencing between assets for which this method is invoked
        private T SwapOldGeneratedAssetWithWorkingAsset<T>(T asset, T referenceAsset) where T : UnityEngine.Object
        {
            string generatedAssetPath = Path.Combine(
                generatedPath,
                $"EZFXLayer_{Path.GetFileName(AssetDatabase.GetAssetPath(referenceAsset))}");

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
        //no need to remove anything, since the working controller is in-memory, and we add subassets at the end
        //TODO: verify this is actually to, because it's probably not
        //though it's really not a big deal to leave what will likely be few subassets around
        void IAssetRepository.FXAnimatorControllerStateRemoved(AnimatorState animatorState) { }
        void IAssetRepository.FXAnimatorStateMachineAdded(AnimatorStateMachine stateMachine)
            => generatedControllerSubassets.Add(stateMachine);
        void IAssetRepository.FXAnimatorTransitionAdded(AnimatorStateTransition transition)
            => generatedControllerSubassets.Add(transition);
    }
}
