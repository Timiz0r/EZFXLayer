namespace EZFXLayer.Test
{
    using System;
    using EZFXLayer;
    using UnityEngine;

    public class ReferenceAnimationConfigurationBuilder
    {
        private readonly AnimatorLayerConfiguration layer;

        public ReferenceAnimationConfigurationBuilder(string name, AnimatorLayerConfiguration layer)
        {
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            if (!string.IsNullOrEmpty(name))
            {
                layer.referenceAnimation.name = name;
            }
            this.layer = layer;
        }

        public ReferenceAnimationConfigurationBuilder AddGameObject(GameObject gameObject, bool isActive)
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            layer.referenceAnimation.gameObjects.Add(
                new AnimatableGameObject() { gameObject = gameObject, active = isActive });

            return this;
        }

        public ReferenceAnimationConfigurationBuilder AddBlendShape(
            SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName, float value)
        {
            if (skinnedMeshRenderer == null) throw new ArgumentNullException(nameof(skinnedMeshRenderer));
            if (string.IsNullOrEmpty(blendShapeName)) throw new ArgumentNullException(nameof(blendShapeName));

            layer.referenceAnimation.blendShapes.Add(new AnimatableBlendShape()
            {
                skinnedMeshRenderer = skinnedMeshRenderer,
                name = blendShapeName,
                value = value
            });

            return this;
        }

        public ReferenceAnimationConfigurationBuilder MakeDefaultAnimation()
        {
            foreach (AnimationConfiguration animation in layer.animations)
            {
                animation.isDefaultAnimation = false;
            }
            layer.referenceAnimation.isDefaultAnimation = true;

            return this;
        }

        public ReferenceAnimationConfigurationBuilder WithStateName(string stateName)
        {
            layer.referenceAnimation.animatorStateNameOverride = stateName;

            return this;
        }
    }
}
