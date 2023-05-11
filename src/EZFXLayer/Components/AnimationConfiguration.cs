namespace EZUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class AnimationConfiguration
    {
        public string key = Guid.NewGuid().ToString();
        public string name;
        public string customAnimatorStateName;
        public string customToggleName;
        public bool isReferenceAnimation;
        public bool isDefaultAnimation;
        public List<AnimatableBlendShape> blendShapes = new List<AnimatableBlendShape>();

        public List<AnimatableGameObject> gameObjects = new List<AnimatableGameObject>();
        public bool isFoldedOut = true;
    }
}
