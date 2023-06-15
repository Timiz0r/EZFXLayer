namespace EZUtils.EZFXLayer
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

        //in the event these are for the reference itself, wont be used
        public bool synchronizeValueWithReference;
        public bool disabled;

        //for unity
        private AnimatableBlendShape() { }

        public AnimatableBlendShape(
            SkinnedMeshRenderer skinnedMeshRenderer,
            string name,
            float value,
            bool synchronizeValueWithReference,
            bool disabled)
        {
            this.skinnedMeshRenderer = skinnedMeshRenderer;
            this.name = name;
            this.value = value;
            this.synchronizeValueWithReference = synchronizeValueWithReference;
            this.disabled = disabled;
        }

        public bool Matches(AnimatableBlendShape blendShape) => blendShape == null
            ? throw new ArgumentNullException(nameof(blendShape))
            : key == blendShape.key;

        public AnimatableBlendShape Clone()
            => new AnimatableBlendShape(
                skinnedMeshRenderer: skinnedMeshRenderer,
                name: name,
                value: value,
                synchronizeValueWithReference: synchronizeValueWithReference,
                disabled: disabled)
            {
                key = key
            };
    }
}
