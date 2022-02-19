namespace EZFXLayer
{
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("EZFXLayer/EZFXLayer animator layer configuration")]
    public class AnimatorLayerConfiguration : MonoBehaviour
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
}
