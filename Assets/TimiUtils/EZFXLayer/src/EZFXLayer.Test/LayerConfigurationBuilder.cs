namespace EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using EZFXLayer;
    using UnityEngine;

    public class LayerConfigurationBuilder
    {
        private readonly AnimatorLayerConfiguration layer;
        public LayerConfigurationBuilder(GameObject gameObject, string name, List<AnimatorLayerConfiguration> layers)
        {
            if (layers == null) throw new ArgumentNullException(nameof(layers));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            layer = gameObject?.AddComponent<AnimatorLayerConfiguration>()
                ?? throw new ArgumentNullException(nameof(gameObject));
            layer.name = name;
            layers.Add(layer);
        }

        public LayerConfigurationBuilder ConfigureReferenceAnimation(Action<ReferenceAnimationConfigurationBuilder> builder)
            => ConfigureReferenceAnimation(null, builder);

        public LayerConfigurationBuilder ConfigureReferenceAnimation(
            string name, Action<ReferenceAnimationConfigurationBuilder> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            ReferenceAnimationConfigurationBuilder b = new ReferenceAnimationConfigurationBuilder(name, layer);
            builder(b);

            return this;
        }

        public LayerConfigurationBuilder AddAnimation(string name, Action<AnimationConfigurationBuilder> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            AnimationConfigurationBuilder b = new AnimationConfigurationBuilder(name, layer);
            builder(b);

            return this;
        }

        public LayerConfigurationBuilder DisableStateManagement()
        {
            layer.manageAnimatorControllerStates = false;

            return this;
        }

        public LayerConfigurationBuilder WithMenuPath(string menuPath)
        {
            layer.menuPath = menuPath;

            return this;
        }
    }
}
