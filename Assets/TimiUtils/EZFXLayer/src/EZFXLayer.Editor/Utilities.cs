namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

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
                    //TODO: eventually want a better exception to indicate what path to look at
                    if (currentMenu.controls.Count > 8) throw new InvalidOperationException(
                        "Cannot add a new sub menu because there are already 8 items in its parent." +
                        $"Menu: {string.Join(" -> ", accumulatedMenuPaths)}");

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
    }
}
