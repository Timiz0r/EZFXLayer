namespace EZFXLayer
{
    using System;
    using UnityEngine;

    [Serializable]
    public class AnimatableBlendShape
    {
        //serializes just fine
#pragma warning disable CA2235
        public SkinnedMeshRenderer skinnedMeshRenderer;
#pragma warning restore CA2235
        public string name;
        public float value;

        public bool Matches(AnimatableBlendShape blendShape) => blendShape == null
            ? throw new ArgumentNullException(nameof(blendShape))
            : skinnedMeshRenderer == blendShape.skinnedMeshRenderer && name == blendShape.name;

        public AnimatableBlendShape Clone() => new AnimatableBlendShape()
        {
            skinnedMeshRenderer = skinnedMeshRenderer,
            name = name,
            value = value
        };
    }
}
