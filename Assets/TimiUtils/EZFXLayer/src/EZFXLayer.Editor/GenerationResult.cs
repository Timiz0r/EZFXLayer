namespace EZFXLayer
{
    using System.Collections.Generic;
    using UnityEngine;

    public class GenerationResult
    {
        public GenerationResult(IReadOnlyList<GeneratedClip> generatedClips)
        {
            GeneratedClips = generatedClips;
        }

        public IReadOnlyList<GeneratedClip> GeneratedClips { get; }
    }
}
