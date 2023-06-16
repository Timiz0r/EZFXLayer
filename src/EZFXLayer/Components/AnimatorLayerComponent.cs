namespace EZUtils.EZFXLayer
{
    using System.Collections.Generic;
    using UnityEngine;

    [ExecuteAlways]
    public class AnimatorLayerComponent : MonoBehaviour
    {
        public new string name;

        public ReferenceAnimatables referenceAnimatables = new ReferenceAnimatables();
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
        //not currently used due to increasing complexity of configuration and behavior
        public bool hideUnchangedItemsInAnimationConfigurations = false;

        [System.Obsolete("Has been split into referenceAnimatables and an additional animation.")]
        //we set a new one, even when obsolete, because serialization does it anyway
        //as such, our code expects it to be non-null, which doesn't work well for unit tests that dont serialize it
        public AnimationConfiguration referenceAnimation = new AnimationConfiguration();

        private AnimatorLayerComponent() { }

        //for the initial value of the component
        public void Reset() => name = name ?? gameObject.name;
    }
}
