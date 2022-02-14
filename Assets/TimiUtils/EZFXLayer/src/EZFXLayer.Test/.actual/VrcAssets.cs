namespace EZFXLayer.Test
{
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class VrcAssets
    {
        private AnimatorController startingFXController = new AnimatorController();
        private VRCExpressionsMenu startMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
        private VRCExpressionParameters startingParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();

        public AnimatorController FXController { get; } = new AnimatorController();
        public VRCExpressionsMenu Menu { get; } = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
        public VRCExpressionParameters Parameters { get; } = ScriptableObject.CreateInstance<VRCExpressionParameters>();


    }
}
