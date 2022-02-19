namespace EZFXLayer
{
    using System.Collections.Generic;
    using UnityEngine;

    public class AnimatorLayerConfiguration
    {
        //TODO: allow it to have a custom name after all
        public AnimationConfiguration defaultAnimation = new AnimationConfiguration()
        {
            name = "Default"
        };
        public List<AnimationConfiguration> animations = new List<AnimationConfiguration>();

        //parameter name to be layerName
        public bool manageStateMachine = true;
        public string menuPath = null;
        //useful for viewing, in some cases
        public bool hideUnchangedItemsInAnimationSets = false;
    }

    [AddComponentMenu("EZFXLayer/EZFXLayer animator layer configuration")]
    public class AnimatorLayerConfigurationComponent : MonoBehaviour
    {
        public AnimatorLayerConfiguration configuration = new AnimatorLayerConfiguration();
    }
}
