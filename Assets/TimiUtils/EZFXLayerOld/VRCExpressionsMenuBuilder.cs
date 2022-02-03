#if UNITY_EDITOR
namespace TimiUtils.EZFXLayerOld
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;
    public class VRCExpressionsMenuBuilder
    {
        private readonly List<Entry> entries = new List<Entry>();

        public void AddEntry(
            string path,
            string item,
            VRCExpressionParameters.Parameter parameter,
            int value)
        {
            if (entries.Any(e => e.Matches(path, item))) throw new Exception(
                $"There is a duplicate menu entry for '{path}' '{item}'.");

            entries.Add(
                new Entry(
                    path,
                    item,
                    new VRCExpressionsMenu.Control.Parameter() { name = parameter.name },
                    value));
        }

        public void Generate(VRCExpressionsMenu rootMenu)
        {
            var assetBasePath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(rootMenu));
            foreach (var entry in entries)
            {
                VRCExpressionsMenu targetMenu = FindOrCreateTargetMenu(rootMenu, entry.Path, assetBasePath);

                targetMenu.controls.Add(new VRCExpressionsMenu.Control()
                {
                    name = entry.Item,
                    parameter = entry.Parameter,
                    type = VRCExpressionsMenu.Control.ControlType.Toggle,
                    value = entry.Value
                });
                EditorUtility.SetDirty(targetMenu);
            }
        }

        private VRCExpressionsMenu FindOrCreateTargetMenu(VRCExpressionsMenu menu, string path, string assetBasePath)
        {
            var match = Regex.Match(
                path,
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
                if (menu.controls.Count > 8) throw new Exception(
                    "Cannot add menu items because there are already 8.");
                return menu;
            }

            var nextLevel = match.Groups[1].Value;
            var rest = match.Groups[2].Value;

            //or should perhaps go with c.subMenu.name? 🤷
            var nextMenu = menu.controls.SingleOrDefault(
                c => c.type == VRCExpressionsMenu.Control.ControlType.SubMenu && c.name == nextLevel)?.subMenu;
            if (nextMenu == null)
            {
                if (menu.controls.Count > 8) throw new Exception(
                    "Cannot add menu items because there are already 8.");

                nextMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                AssetDatabase.CreateAsset(nextMenu, $"{assetBasePath}/EZFXLayer_Menu_{nextLevel}.asset");
                menu.controls.Add(new VRCExpressionsMenu.Control()
                {
                    name = nextLevel,
                    type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                    subMenu = nextMenu
                });
                EditorUtility.SetDirty(menu);
            }
            return FindOrCreateTargetMenu(nextMenu, rest, assetBasePath);
        }

        private class Entry
        {
            public string Path { get; }
            public string Item { get; }
            public VRCExpressionsMenu.Control.Parameter Parameter { get; }
            public int Value { get; }

            public Entry(
                string path,
                string item,
                VRCExpressionsMenu.Control.Parameter parameter,
                int value)
            {
                Path = path;
                Item = item;
                Parameter = parameter;
                Value = value;
            }

            public bool Matches(string path, string item)
                => StringComparer.OrdinalIgnoreCase.Equals(Path, path)
                    && StringComparer.OrdinalIgnoreCase.Equals(Item, item);
        }
    }
}
#endif
