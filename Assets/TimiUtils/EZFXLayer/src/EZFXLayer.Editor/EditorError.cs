namespace EZFXLayer
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public static class EditorError
    {
        public static void Display(string message) => Display(message, () => { });

        public static void Display(string message, Action undoCallback)
        {
            Debug.LogError(message);

            if (!EditorUtility.DisplayDialog("EZFXLayer", message, "OK", "Undo"))
            {
                undoCallback();
            }
        }
    }
}
