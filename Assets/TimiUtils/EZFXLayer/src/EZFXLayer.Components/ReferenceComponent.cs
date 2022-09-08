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
        public VRCExpressionParameters vrcExpressionParameters;
        public VRCExpressionsMenu vrcRootExpressionsMenu;
        public bool generateOnUpload = true;

        private void Reset()
        {
            ReferenceComponent[] allComponents = FindObjectsOfType<ReferenceComponent>();
            if (allComponents.Length == 1) return;

            string parentNames = string.Join(", ", allComponents.Select(c => c.gameObject.name));
            //TODO: want to eventually allow avatars to have their own configuration, instead of scene-wide
            //for reference configuration, avatars basically override
            //for layer configuration, not yet sure which gets applied first.
            //  a natural feeling way would be for avatars to go last, since lower layers override upper layers,
            //      and avatar config is overriding scene config. it also makes it natural to have marker layers based
            //      on what's in the scene config, where it would be unnatural the other way around in a scene with
            //      many avatars.
            //  this is lower priority tho, since i personally don't care about unusable menu items. this would certainly
            //      increase quality of the product tho!
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
