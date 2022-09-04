namespace EZFXLayer.Test
{
    using EZFXLayer;

    public class AnimationConfigurationBuilder
    {
        private AnimationConfiguration animation;

        public AnimationConfigurationBuilder(string name, AnimatorLayerConfiguration layer)
        {
            layer.animations.Add(animation = new AnimationConfiguration() { name = name });
        }
    }
}
