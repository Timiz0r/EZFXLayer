namespace EZFXLayer.Test
{
    using System;
    using EZFXLayer;

    public class DefaultAnimationConfigurationBuilder
    {
        private AnimatorLayerConfiguration layer;

        public DefaultAnimationConfigurationBuilder(string name, AnimatorLayerConfiguration layer)
        {
            if (layer == null) throw new ArgumentNullException(nameof(layer));
            if (!string.IsNullOrEmpty(name))
            {
                layer.defaultAnimation.name = name;
            }
            this.layer = layer;
        }
    }
}
