namespace EZFXLayer
{
    using System;
    using UnityEditor;
    using UnityEngine;

    internal static class EditorError
    {
        public static void Display(string message) => Display(message, null);

        public static void Display(string message, Action undoCallback)
        {
            Debug.LogError(message);

            if (undoCallback == null)
            {
                _ = EditorUtility.DisplayDialog("EZFXLayer", message, "OK");
            }
            else if (!EditorUtility.DisplayDialog("EZFXLayer", message, "OK", "Undo"))
            {
                undoCallback();
            }
        }
    }
}
