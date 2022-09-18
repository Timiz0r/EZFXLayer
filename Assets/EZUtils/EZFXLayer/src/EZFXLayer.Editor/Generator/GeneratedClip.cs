namespace EZUtils.EZFXLayer
{
    using UnityEngine;

    public class GeneratedClip
    {
        public string LayerName { get; }
        public string AnimationName { get; }
        public AnimationClip Clip { get; }

        public GeneratedClip(string layerName, string animationName, AnimationClip clip)
        {
            LayerName = layerName;
            AnimationName = animationName;
            Clip = clip;
        }
    }
}
