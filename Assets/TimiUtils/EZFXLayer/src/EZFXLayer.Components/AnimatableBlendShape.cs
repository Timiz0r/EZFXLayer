namespace EZFXLayer
{
    using System;
    using UnityEngine;

    [Serializable]
    public class AnimatableBlendShape
    {
        public string key = Guid.NewGuid().ToString(); //shared across reference animation and other animations

        public SkinnedMeshRenderer skinnedMeshRenderer;
        public string name;
        public float value;

        public bool Matches(AnimatableBlendShape blendShape) => blendShape == null
            ? throw new ArgumentNullException(nameof(blendShape))
            : key == blendShape.key;

        public AnimatableBlendShape Clone() => new AnimatableBlendShape()
        {
            key = key,
            skinnedMeshRenderer = skinnedMeshRenderer,
            name = name,
            value = value
        };
    }
}
