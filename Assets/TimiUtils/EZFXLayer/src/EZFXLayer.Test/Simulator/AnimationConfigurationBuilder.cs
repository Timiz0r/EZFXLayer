namespace EZFXLayer.Test
{
    using System;
    using System.Linq;
    using EZFXLayer;
    using UnityEngine;

    public class AnimationConfigurationBuilder
    {
        private readonly AnimatorLayerComponent layer;
        private readonly AnimationConfiguration animation;

        public AnimationConfigurationBuilder(string name, AnimatorLayerComponent layer)
        {
            if (layer == null) throw new ArgumentNullException(nameof(layer));

            layer.animations.Add(animation = new AnimationConfiguration() { name = name });
            this.layer = layer;
        }

        public AnimationConfigurationBuilder SetGameObject(GameObject gameObject, bool isActive)
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            if (!layer.referenceAnimation.gameObjects.Any(go => go.gameObject == gameObject))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gameObject), $"GameObject '{gameObject}' is not in the reference animation.");
            }

            animation.gameObjects.Add(new AnimatableGameObject() { gameObject = gameObject, active = isActive });

            return this;
        }

        public AnimationConfigurationBuilder SetBlendShape(
            SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName, float value)
        {
            if (skinnedMeshRenderer == null) throw new ArgumentNullException(nameof(skinnedMeshRenderer));
            if (string.IsNullOrEmpty(blendShapeName)) throw new ArgumentNullException(nameof(blendShapeName));

            if (!layer.referenceAnimation.blendShapes.Any(bs =>
                bs.skinnedMeshRenderer == skinnedMeshRenderer
                && bs.name.Equals(blendShapeName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(blendShapeName), $"Blend shape '{blendShapeName}' is not in the reference animation.");
            }

            animation.blendShapes.Add(new AnimatableBlendShape()
            {
                skinnedMeshRenderer = skinnedMeshRenderer,
                name = blendShapeName,
                value = value
            });

            return this;
        }

        public AnimationConfigurationBuilder MakeDefaultAnimation()
        {
            layer.referenceAnimation.isDefaultAnimation = false;
            foreach (AnimationConfiguration animation in layer.animations)
            {
                animation.isDefaultAnimation = false;
            }
            animation.isDefaultAnimation = true;

            return this;
        }

        public AnimationConfigurationBuilder WithToggleName(string toggleName)
        {
            animation.customToggleName = toggleName;

            return this;
        }
    }
}
