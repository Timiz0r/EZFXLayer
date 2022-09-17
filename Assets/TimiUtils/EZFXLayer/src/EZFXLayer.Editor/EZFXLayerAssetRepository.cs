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
        private readonly string workingPath;
        private readonly string generatedPath;

        private readonly AnimatorController referenceController;
        private readonly VRCExpressionsMenu referenceMenu;
        private readonly VRCExpressionParameters referenceParameters;
        private readonly List<GeneratedClip> generatedClips = new List<GeneratedClip>();
        private readonly List<GeneratedMenu> generatedMenus = new List<GeneratedMenu>();

        private AnimatorController workingController;
        private VRCExpressionsMenu workingMenu;
        private VRCExpressionParameters workingParameters;

        public EZFXLayerAssetRepository(
            string outputPath,
            AnimatorController referenceController,
            VRCExpressionsMenu referenceMenu,
            VRCExpressionParameters referenceParameters)
        {
            workingPath = Path.Combine(outputPath, "working");
            generatedPath = Path.Combine(outputPath, "generated");
            this.referenceController = referenceController;
            this.referenceMenu = referenceMenu;
            this.referenceParameters = referenceParameters;
        }

        public (AnimatorController, VRCExpressionsMenu, VRCExpressionParameters) PrepareWorkingAssets()
        {
            _ = AssetDatabase.DeleteAsset(workingPath);

            _ = EnsureFolderCreated(workingPath);
            _ = EnsureFolderCreated(generatedPath);

            return
            (
                //it's actually rather hard to duplicate an animator controller without copying the asset
                //but if we want to, it's relatively easy to just copy in-memory for others
                //also note that we can't StartEditingAssets yet
                workingController = GetWorkingAssetCopy(referenceController),
                workingMenu = GetWorkingAssetCopy(referenceMenu),
                workingParameters = GetWorkingAssetCopy(referenceParameters)
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
                AnimatorController generatedController = SwapOldGeneratedAssetWithWorkingAsset(workingController);
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
        private T SwapOldGeneratedAssetWithWorkingAsset<T>(T asset) where T : UnityEngine.Object
        {
            string workingAssetPath = AssetDatabase.GetAssetPath(asset);
            string generatedAssetPath = Path.Combine(generatedPath, Path.GetFileName(workingAssetPath));

            string generatedAssetGuid = AssetDatabase.AssetPathToGUID(generatedAssetPath);
            _ = AssetDatabase.DeleteAsset(generatedAssetPath);

            //would expect both to be null or both to be non-null, but we'll check both anyway
            string workingAssetMetaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(workingAssetPath);
            if (!string.IsNullOrEmpty(generatedAssetGuid) && !string.IsNullOrEmpty(workingAssetMetaFilePath))
            {
                string workingAssetGuid = AssetDatabase.AssetPathToGUID(workingAssetPath);
                string metaFileContents = File.ReadAllText(workingAssetMetaFilePath);
                string newMetaFileContents = metaFileContents.Replace(workingAssetGuid, generatedAssetGuid);
                File.WriteAllText(workingAssetMetaFilePath, newMetaFileContents);
            }

            _ = AssetDatabase.MoveAsset(workingAssetPath, generatedAssetPath);

            return asset;
        }

        private T GetWorkingAssetCopy<T>(T original) where T : UnityEngine.Object
        {
            string originalPath = AssetDatabase.GetAssetPath(original);

            string newPath = $"{workingPath}/EZFXLayer_{Path.GetFileName(originalPath)}";
            if (!AssetDatabase.CopyAsset(originalPath, newPath))
            {
                throw new Exception($"Error copying base '{typeof(T)}' at '{originalPath}' to '{newPath}'.");
            }

            T newAsset = AssetDatabase.LoadAssetAtPath<T>(newPath);
            return newAsset;
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
            => AssetDatabase.AddObjectToAsset(animatorState, workingController);
        void IAssetRepository.FXAnimatorControllerStateRemoved(AnimatorState animatorState)
            => AssetDatabase.RemoveObjectFromAsset(animatorState);
        void IAssetRepository.FXAnimatorStateMachineAdded(AnimatorStateMachine stateMachine)
            => AssetDatabase.AddObjectToAsset(stateMachine, workingController);
        void IAssetRepository.FXAnimatorTransitionAdded(AnimatorStateTransition transition)
            => AssetDatabase.AddObjectToAsset(transition, workingController);
    }
}
