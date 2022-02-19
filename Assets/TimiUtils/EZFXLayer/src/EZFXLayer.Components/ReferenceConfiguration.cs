namespace EZFXLayer
{
    using System;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class ReferenceConfiguration
    {
        public AnimatorController fxLayerController;
        public VRCExpressionParameters vrcExpressionParameters;
        public VRCExpressionsMenu vrcRootExpressionsMenu;
        public bool generateOnUpload = true;
    }

    [AddComponentMenu("EZFXLayer/EZFXLayer reference configuration")]
    public class ReferenceConfigurationComponent : MonoBehaviour
    {
        //TODO: in the event that it cant be deserialized properly to the right reference because of the interfaces
        //we'll basically do some converting from the component to the above class

        // public AnimatorController fxLayerController;
        // public VRCExpressionParameters vrcExpressionParameters;
        // public VRCExpressionsMenu vrcRootExpressionsMenu;
        public ReferenceConfiguration configuration = new ReferenceConfiguration();

        // private void Reset()
        // {
        //     ReferenceConfiguration[] allComponents = FindObjectsOfType<ReferenceConfiguration>();
        //     if (allComponents.Length == 1) return;

        //     string parentNames = string.Join(", ", allComponents.Select(c => c.gameObject.name));
        //     throw new InvalidOperationException(
        //         $"Only one {nameof(ReferenceConfiguration)} should exist per scene." +
        //         $"Game objects with the component: {parentNames}.");
        //     //crashes unity if the reset is clicked. works fine on first add tho.
        //     //but if we bring this back, use the DisplayError overload
        //     // if (!EditorUtility.DisplayDialog(
        //     //     "EZFXLayer", $"Only one {nameof(EZFXLayerRootConfiguration)} should exist per scene.", "OK", "Undo"))
        //     // {
        //     //     DestroyImmediate(this);
        //     // }
        // }
    }
}
