namespace EZFXLayer
{
    using System;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    [AddComponentMenu("EZFXLayer/EZFXLayer reference configuration")]
    public class ReferenceComponent : MonoBehaviour
    {
        public AnimatorController fxLayerController;
        public VRCExpressionsMenu vrcRootExpressionsMenu;
        public VRCExpressionParameters vrcExpressionParameters;
        public bool generateOnUpload = true;

        private void Reset()
        {
            ReferenceComponent[] allComponents = FindObjectsOfType<ReferenceComponent>();
            if (allComponents.Length == 1) return;

            string parentNames = string.Join(", ", allComponents.Select(c => c.gameObject.name));

            throw new InvalidOperationException(
                $"Only one {nameof(ReferenceComponent)} should exist per scene." +
                $"Game objects with the component: {parentNames}.");
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
