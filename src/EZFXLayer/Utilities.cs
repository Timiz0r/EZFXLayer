namespace EZUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    using static Localization;

    internal static class Utilities
    {
        public static string GetRelativePath(this GameObject gameObject)
        {
            List<string> names = new List<string>();
            while (gameObject != null && gameObject.GetComponent<VRCAvatarDescriptor>() == null)
            {
                names.Add(gameObject.name);
                gameObject = gameObject.transform.parent.gameObject;
            }
            names.Reverse();
            string result = string.Join("/", names);
            return result;
        }

        //it's a design choice to basically not create assets in the generator and let its driver adapter do that
        //but this is fine to do if we can, and it incidentally matches the internal code of AnimatorController
        public static void TryAddObjectToAsset(UnityEngine.Object objectToAdd, UnityEngine.Object potentialAsset)
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(potentialAsset))) return;
            AssetDatabase.AddObjectToAsset(objectToAdd, potentialAsset);
        }

        public static VRCExpressionsMenu FindOrCreateTargetMenu(
            VRCExpressionsMenu rootMenu,
            string path,
            IAssetRepository assetRepository)
        {
            //splits on forwards slashes not preceeded by an odd number of backslashes
            // \\/foo -> if even, then the forward slash is a valid path separator
            // \\\/foo -> if odd, then there will be a backslash left to escape the forward slash
            IEnumerable<string> pathParts =
                Regex.Split(path ?? string.Empty, @"(?<!(?<!\\)\\(?:\\\\)*)/")
                    .Select(p => Regex.Replace(p, @"\\([/\\])", "$1"));
            VRCExpressionsMenu currentMenu = rootMenu;
            List<string> accumulatedMenuPaths = new List<string>(); //for exceptions
            foreach (string pathPart in pathParts)
            {
                //so leading, trailing, and redundant slashes we'll ignore
                //for instance, /////foo/////// is treated as foo
                if (string.IsNullOrEmpty(pathPart)) continue;
                //should be placed outside of creation ofc, and should be early enough that GeneratedMenu has right val
                accumulatedMenuPaths.Add(pathPart);

                VRCExpressionsMenu nextMenu = currentMenu.controls.SingleOrDefault(
                    c => c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.name == pathPart)?.subMenu;
                if (nextMenu == null)
                {
                    if (currentMenu.controls.Count >= 8) throw new InvalidOperationException(
                        T("Cannot add a new sub menu because there are already 8 items in its parent.") +
                        T($"Menu: {string.Join(" -> ", accumulatedMenuPaths)}"));

                    nextMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                    currentMenu.controls.Add(new VRCExpressionsMenu.Control()
                    {
                        name = pathPart,
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = nextMenu
                    });
                    assetRepository.VRCSubMenuAdded(new GeneratedMenu(accumulatedMenuPaths.ToArray(), nextMenu));
                }
                currentMenu = nextMenu;
            }

            return currentMenu;
        }

        public static void RecordChange<T>(T target, string operationDescription, Action<T> changer)
            where T : UnityEngine.Object
        {
            Undo.RecordObject(target, operationDescription);
            changer(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        public static IEnumerable<SerializedProperty> GetArrayElements(this SerializedProperty array)
            => Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i));

        public static void ForEachArrayElement(
            this SerializedProperty array, Action<SerializedProperty> action)
                => array.ForEachArrayElement((i, p) => action(p));

        public static void ForEachArrayElement(
            this SerializedProperty array, Action<int, SerializedProperty> action)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty current = array.GetArrayElementAtIndex(i);

                action(i, current);
            }
        }

        public static IEnumerable<string> GetBlendShapeNames(this SkinnedMeshRenderer smr)
            => Enumerable.Range(0, smr.sharedMesh.blendShapeCount).Select(i => smr.sharedMesh.GetBlendShapeName(i));

        //have not found an ideal way to call this via unity messaging in the component
        //so we call it in the two places that matter most: inspector and generation
        //decided against having undo records for these conversions, since we dont maintain code that uses
        //these old fields
        public static void PerformComponentUpgrades(this AnimatorLayerComponent layer, out bool changePerformed)
        {
            changePerformed = false;

            //since we cant do a null check, we reuse isReferenceAnimation, which used to be true for the reference anim
            //but doing away with them, setting it to false is a logical way to indicate otherwise
#pragma warning disable CS0618 // Type or member is obsolete
            if (layer.referenceAnimation.isReferenceAnimation)
            {
                AnimationConfiguration copiedAnimation = new AnimationConfiguration()
                {
                    key = layer.referenceAnimation.key,
                    name = layer.referenceAnimation.name,
                    customAnimatorStateName = layer.referenceAnimation.customAnimatorStateName,
                    customToggleName = layer.referenceAnimation.customToggleName,
                    isReferenceAnimation = false,
                    isDefaultAnimation = layer.referenceAnimation.isDefaultAnimation,
                    isFoldedOut = layer.referenceAnimation.isFoldedOut,
                    blendShapes = layer.referenceAnimation.blendShapes.Select(bs => bs.Clone()).ToList(),
                    gameObjects = layer.referenceAnimation.gameObjects.Select(go => go.Clone()).ToList(),
                };
                layer.animations.Insert(0, copiedAnimation);

                layer.referenceAnimatables.blendShapes =
                    layer.referenceAnimation.blendShapes.Select(bs => bs.Clone()).ToList();
                layer.referenceAnimatables.gameObjects =
                    layer.referenceAnimation.gameObjects.Select(go => go.Clone()).ToList();
                layer.referenceAnimatables.isFoldedOut = layer.referenceAnimation.isFoldedOut;

                layer.referenceAnimation.isReferenceAnimation = false;
                changePerformed = true;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            if (changePerformed)
            {
                EditorUtility.SetDirty(layer);
                PrefabUtility.RecordPrefabInstancePropertyModifications(layer);
            }
        }
    }
}
