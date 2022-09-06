namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class ClipManifest
    {
        private readonly Dictionary<string, AnimationClip> clipMap =
            new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);

        public void Add(string animationName, AnimationClip clip)
        {
            if (clipMap.ContainsKey(animationName)) throw new InvalidOperationException(
                $"An animation named '{animationName}' has already been generated.");
            clipMap[animationName] = clip;
        }

        public bool TryFind(string animationName, out AnimationClip clip)
            => clipMap.TryGetValue(animationName, out clip);
    }
}
