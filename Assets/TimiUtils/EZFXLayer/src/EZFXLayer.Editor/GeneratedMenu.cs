namespace EZFXLayer
{
    using System.Collections.Generic;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class GeneratedMenu
    {
        public IReadOnlyList<string> PathComponents { get; }
        public VRCExpressionsMenu Menu { get; }

        public GeneratedMenu(IReadOnlyList<string> pathComponents, VRCExpressionsMenu menu)
        {
            PathComponents = pathComponents;
            Menu = menu;
        }
    }
}
