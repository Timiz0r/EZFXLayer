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

        public LayerConfigurationBuilder ConfigureDefaultAnimation(Action<DefaultAnimationConfigurationBuilder> builder)
            => ConfigureDefaultAnimation(null, builder);

        public LayerConfigurationBuilder ConfigureDefaultAnimation(
            string name, Action<DefaultAnimationConfigurationBuilder> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            DefaultAnimationConfigurationBuilder b = new DefaultAnimationConfigurationBuilder(name, layer);
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
    }
}
