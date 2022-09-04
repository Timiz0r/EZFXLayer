namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class ClipManifest
    {
        private readonly string layerName;
        private readonly Dictionary<string, AnimationClip> clipMap =
            new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
        private readonly List<GeneratedClip> clips = new List<GeneratedClip>();

        public ClipManifest(AnimatorLayerConfiguration layer)
        {
            layerName = layer?.name ?? throw new ArgumentOutOfRangeException(nameof(layer), "Layer has no name.");
        }

        public IReadOnlyList<GeneratedClip> Clips => clips;

        public void Add(string animationName, AnimationClip clip)
        {
            if (clipMap.ContainsKey(animationName)) throw new InvalidOperationException(
                $"An animation named '{animationName}' has already been generated.");
            clipMap[animationName] = clip;
            clips.Add(new GeneratedClip(layerName, animationName, clip));
        }

        public bool TryFind(string animationName, out AnimationClip clip)
            => clipMap.TryGetValue(animationName, out clip);
    }
}
