namespace EZUtils.EZFXLayer
{
    using System;
    using System.Linq;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    using static Localization;

    public class ReferenceComponent : MonoBehaviour
    {
        //this has to be a RuntimeAnimatorController because the AnimatorController we really want is in UnityEditor
        //which we can't reference or else we cant build (well, not without annoying conditional compilation)
        public RuntimeAnimatorController fxLayerController;
        public VRCExpressionsMenu vrcRootExpressionsMenu;
        public VRCExpressionParameters vrcExpressionParameters;
        public bool generateOnUpload = true;
        public GenerationOptions generationOptions;

        private void Reset()
        {
            ReferenceComponent[] allComponents = FindObjectsOfType<ReferenceComponent>();
            if (allComponents.Length == 1) return;

            string parentNames = string.Join(", ", allComponents.Select(c => c.gameObject.name));

            throw new InvalidOperationException(
                T($"Only one {nameof(ReferenceComponent)} should exist per scene.") +
                T($"Game objects with the component: {parentNames}."));
            //crashes unity if the reset is clicked. works fine on first add tho.
            //but if we bring this back, use the DisplayError overload
            // if (!EditorUtility.DisplayDialog(
            //     "EZFXLayer", $"Only one {nameof(EZFXLayerRootConfiguration)} should exist per scene.", "OK", "Undo"))
            // {
            //     DestroyImmediate(this);
            // }
        }
    }
}
