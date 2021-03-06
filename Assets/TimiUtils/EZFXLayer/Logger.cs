using System;
using UnityEditor;
using UnityEngine;

namespace TimiUtils.EZFXLayer
{
    public static class Logger
    {
        public static void DisplayError(string message)
        {
            Debug.LogError(message);

            EditorUtility.DisplayDialog("EZFXLayer", message, "OK");
        }

        public static void DisplayError(string message, Action undoCallback)
        {
            Debug.LogError(message);

            if (!EditorUtility.DisplayDialog("EZFXLayer", message, "OK", "Undo"))
            {
                undoCallback();
            }
        }
    }
}
