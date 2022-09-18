namespace EZUtils.EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    //TODO: this could hypothetically go into the editor assembly, allowing those to author these configs as code isntead of components
    //with decent redesign to interact with a scene, anyway. wonder if tests will try to do that :thinking:
    public class ConfigurationBuilder
    {
        private readonly List<AnimatorLayerComponent> layers = new List<AnimatorLayerComponent>();
        //not allowed to instantiate a MonoBehavior like ReferenceConfiguration
        private readonly GameObject gameObject;
        private readonly IAssetRepository assetRepository;
        private readonly GenerationOptions generationOptions = new GenerationOptions();

        public ConfigurationBuilder(GameObject gameObject, IAssetRepository assetRepository)
        {
            this.gameObject = gameObject;
            this.assetRepository = assetRepository;
        }

        public EZFXLayerConfiguration Generate()
            => new EZFXLayerConfiguration(
                layers.Select(l => AnimatorLayerConfiguration.FromComponent(l, generationOptions)).ToArray(),
                assetRepository);

        public ConfigurationBuilder AddLayer(string name) => AddLayer(name, l => { });

        public ConfigurationBuilder AddLayer(string name, Action<LayerConfigurationBuilder> builder)
        {
            //or could handle it, but we have another overload anyway
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            LayerConfigurationBuilder b = new LayerConfigurationBuilder(gameObject, name, layers);
            builder(b);

            return this;
        }

        public ConfigurationBuilder WriteDefaultValues(bool enabled)
        {
            generationOptions.setWriteDefaultValues = enabled;
            return this;
        }
    }
}
