namespace EZFXLayer
{
    using System.Collections.Generic;

    public class EZFXLayerConfiguration
    {
        public IReadOnlyList<AnimatorLayerConfiguration> Layers { get; }
        public IAssetRepository AssetRepository { get; }

        public EZFXLayerConfiguration(
            IReadOnlyList<AnimatorLayerConfiguration> layers,
            IAssetRepository assetRepository)
        {
            Layers = layers;
            AssetRepository = assetRepository;
        }
    }
}
