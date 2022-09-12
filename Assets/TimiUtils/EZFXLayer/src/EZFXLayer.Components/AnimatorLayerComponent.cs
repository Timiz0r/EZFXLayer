namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.Animations;
    using UnityEngine;

    [AddComponentMenu("EZFXLayer/EZFXLayer animator layer configuration")]
    public class AnimatorLayerComponent : MonoBehaviour
    {
        //TODO: verify that shadowing isnt a problem.
        //want to try it here because it's confusing having an unusable name field
        public new string name;

        public AnimationConfiguration referenceAnimation = new AnimationConfiguration()
        {
            name = "Default",
            isDefaultAnimation = true,
            isReferenceAnimation = true //wont add editor support to change this
        };
        public List<AnimationConfiguration> animations = new List<AnimationConfiguration>();

        //field name to be layerName
        public bool manageAnimatorControllerStates = true;
        public bool manageExpressionMenuAndParameters = true;
        //no reasonable scenario in which we won't want to generate animations (for non-empty AnimationConfiguration)
        //TODO: well maybe if bringing own clips, but shall eventually support that maybe? low pri and needs thought
        //  but really just manually create the layer. if a mix of ezfxlayer and custom clips are needed, then put own
        //  clips in a separate layer using the same parameters
        //public bool manageAnimations = true;
        public string menuPath = null;
        //useful for viewing, in some cases
        public bool hideUnchangedItemsInAnimationConfigurations = false;

        // private void OnValidate()
        // {
        //     if (referenceAnimation == null)
        //     {
        //         referenceAnimation = ScriptableObject.CreateInstance<AnimationConfiguration>();
        //         referenceAnimation.name = "Default";
        //         //could check for other animations, but this should only happen on first creation
        //         //since we don't allow unsetting it in the editor
        //         referenceAnimation.isDefaultAnimation = true;
        //         //wont add editor support to change this
        //         referenceAnimation.isDefaultState = true;
        //     }
        // }
        //         public AnimationConfiguration referenceAnimation = new AnimationConfiguration()
        // {
        //     name = "Default",
        //     isDefaultAnimation = true,
        //     isDefaultState = true //wont add editor support to change this
        // };
    }
}
