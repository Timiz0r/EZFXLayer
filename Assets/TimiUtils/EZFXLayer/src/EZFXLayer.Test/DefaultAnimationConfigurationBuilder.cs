namespace EZFXLayer.Test
{
    using System;
    using EZFXLayer;
    using UnityEngine;

    public class DefaultAnimationConfigurationBuilder
    {
        private readonly AnimatorLayerConfiguration layer;

        public DefaultAnimationConfigurationBuilder(string name, AnimatorLayerConfiguration layer)
        {
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            if (!string.IsNullOrEmpty(name))
            {
                layer.defaultAnimation.name = name;
            }
            this.layer = layer;
        }

        public DefaultAnimationConfigurationBuilder AddGameObject(GameObject gameObject, bool isActive)
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            layer.defaultAnimation.gameObjects.Add(
                new AnimatableGameObject() { gameObject = gameObject, active = isActive });

            return this;
        }

        public DefaultAnimationConfigurationBuilder AddBlendShape(
            SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName, float value)
        {
            if (skinnedMeshRenderer == null) throw new ArgumentNullException(nameof(skinnedMeshRenderer));
            if (string.IsNullOrEmpty(blendShapeName)) throw new ArgumentNullException(nameof(blendShapeName));

            layer.defaultAnimation.blendShapes.Add(new AnimatableBlendShape()
            {
                skinnedMeshRenderer = skinnedMeshRenderer,
                name = blendShapeName,
                value = value
            });

            return this;
        }
    }
}
