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
        public bool isToggleOffAnimation;
        public List<AnimatableBlendShape> blendShapes = new List<AnimatableBlendShape>();

        public List<AnimatableGameObject> gameObjects = new List<AnimatableGameObject>();
        public bool isFoldedOut = true;

        public static AnimationConfiguration Create(string name) => new AnimationConfiguration() { name = name };
    }
}
