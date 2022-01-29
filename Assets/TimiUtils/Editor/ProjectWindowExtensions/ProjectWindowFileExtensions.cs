using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Utils
{
    public static class ProjectWindowFileExtensions
    {
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            MulticastDelegateHelper.Register(
                ref EditorApplication.projectWindowItemOnGUI,
                nameof(ProjectWindowFileExtensions),
                ForProjectWindowItem);
        }

        private static void ForProjectWindowItem(string guid, Rect selectionRect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path) || Directory.Exists(path))
            {
                return;
            }

            FileInfo file = new FileInfo(path);
            if (string.IsNullOrEmpty(file.Extension))
            {
                return;
            }

            GUIContent labelContent = new GUIContent(file.Extension);
            //fyi `EditorStyles.label` does not appear accessible thru cctor
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.active.textColor = Color.white;

            Vector2 size = labelStyle.CalcSize(labelContent);
            Vector2 predictedFileNameSize = labelStyle.CalcSize(new GUIContent(Path.GetFileNameWithoutExtension(file.Name)));
            const float iconSize = 16;
            Rect labelRect = new Rect(
                new Vector2(
                    selectionRect.x + predictedFileNameSize.x - iconSize + 34, //idk
                    selectionRect.yMin + 1),
                size);

            //EditorGUI.DrawRect(selectionRect, new Color32(255, 255, 255, 30));

            EditorGUI.LabelField(labelRect, labelContent, labelStyle);
        }
    }
}
