namespace EZUtils.EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using EZFXLayer;
    using UnityEngine;

    public class LayerConfigurationBuilder
    {
        private readonly AnimatorLayerComponent layer;
        public LayerConfigurationBuilder(GameObject gameObject, string name, List<AnimatorLayerComponent> layers)
        {
            if (layers == null) throw new ArgumentNullException(nameof(layers));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            layer = gameObject.AddComponent<AnimatorLayerComponent>();
            layer.name = name;
            layers.Add(layer);
        }

        public LayerConfigurationBuilder AddInitialAnimation(Action<AnimationConfigurationBuilder> builder)
            => AddInitialAnimation("Default", builder);

        public LayerConfigurationBuilder AddInitialAnimation(string name, Action<AnimationConfigurationBuilder> builder)
            => AddAnimation(name, ac => builder(ac.MakeDefaultAnimation().MakeToggleOffAnimation()));

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

        public LayerConfigurationBuilder DisableSavedParameters()
        {
            layer.saveExpressionParameters = false;

            return this;
        }

        public LayerConfigurationBuilder WithMenuPath(string menuPath)
        {
            layer.menuPath = menuPath;

            return this;
        }
    }
}
