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
            VRCExpressionsMenu menu,
            string path,
            List<VRCExpressionsMenu> createdMenus)
        {
            Match match = Regex.Match(
                path ?? string.Empty,
                @"
(?>/?) #it fine to start a path with a /. an atomic group is used because, for the '/' case, backtracking will put it in the capture group
(.+?)
(?:
  (?<!\\)/ #can escape a forward slash with a backslash
  (.+)
)?$ #this non-capturing group is optional in case we're on the last element",
                RegexOptions.IgnorePatternWhitespace);

            if (!match.Success)
            {
                return menu.controls.Count > 8
                    ? throw new InvalidOperationException("Cannot add menu items because there are already 8.")
                    : menu;
            }

            string nextLevel = match.Groups[1].Value;
            string rest = match.Groups[2].Value;

            //or should perhaps go with c.subMenu.name? 🤷
            VRCExpressionsMenu nextMenu = menu.controls.SingleOrDefault(
                c => c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.name == nextLevel)?.subMenu;
            if (nextMenu == null)
            {
                if (menu.controls.Count > 8) throw new InvalidOperationException(
                    "Cannot add menu items because there are already 8.");

                nextMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                createdMenus.Add(nextMenu);
                menu.controls.Add(new VRCExpressionsMenu.Control()
                {
                    name = nextLevel,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = nextMenu
                });
            }
            return FindOrCreateTargetMenu(nextMenu, rest, createdMenus);
        }
    }
}
