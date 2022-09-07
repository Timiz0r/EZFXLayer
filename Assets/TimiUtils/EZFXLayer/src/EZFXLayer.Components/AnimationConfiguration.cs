namespace EZFXLayer
{
    using System.Collections.Generic;

    public class AnimationConfiguration
    {
        public string name;
        public string animatorStateNameOverride;
        public string menuNameOverride;
        public bool isDefaultAnimation;
        public List<AnimatableBlendShape> blendShapes = new List<AnimatableBlendShape>();

        public List<AnimatableGameObject> gameObjects = new List<AnimatableGameObject>();
        public bool isFoldedOut = true;

        public string EffectiveStateName
            => string.IsNullOrEmpty(animatorStateNameOverride) ? name : animatorStateNameOverride;

        public string EffectiveMenuName
            => string.IsNullOrEmpty(menuNameOverride) ? name : menuNameOverride;
    }
}
