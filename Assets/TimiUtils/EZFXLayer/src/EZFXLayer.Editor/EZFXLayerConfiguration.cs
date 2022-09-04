namespace EZFXLayer
{
    using System.Collections.Generic;

    public class EZFXLayerConfiguration
    {
        public IReadOnlyList<AnimatorLayerConfiguration> Layers { get; private set; }

        public EZFXLayerConfiguration(IReadOnlyList<AnimatorLayerConfiguration> layers
        )
        {
            Layers = layers;
        }
    }
}
