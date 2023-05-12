namespace EZUtils.EZFXLayer
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    using static Localization;

    public class AnimatorLayerComponent : MonoBehaviour
    {
        public new string name;

        public AnimationConfiguration referenceAnimation = new AnimationConfiguration()
        {
            name = "Default",
            isDefaultAnimation = true,
            isReferenceAnimation = true //wont add editor support to change this
        };
        public List<AnimationConfiguration> animations = new List<AnimationConfiguration>();

        public bool manageAnimatorControllerStates = true;
        public bool manageExpressionMenuAndParameters = true;
        public bool saveExpressionParameters = true;

        //no reasonable scenario in which we won't want to generate animations (for non-empty AnimationConfiguration)
        //TODO: well maybe if bringing own clips, but shall eventually support that maybe? low pri and needs thought
        //  but really just manually create the layer. if a mix of ezfxlayer and custom clips are needed, then put own
        //  clips in a separate layer using the same parameters
        //public bool manageAnimations = true;
        public string menuPath = null;
        public bool hideUnchangedItemsInAnimationConfigurations = false;

        //for the initial value of the component
        public void Reset() => name = name ?? gameObject.name;

    }
}
