namespace EZFXLayer
{
    using System.Collections.Generic;

    public class EZFXLayerConfiguration
    {
        public ReferenceConfiguration Reference { get; private set; }
        public IReadOnlyList<AnimatorLayerConfiguration> Layers { get; private set; }

        public EZFXLayerConfiguration(
            ReferenceConfiguration reference, IReadOnlyList<AnimatorLayerConfiguration> layers
        )
        {
            Reference = reference;
            Layers = layers;
        }
    }
}
