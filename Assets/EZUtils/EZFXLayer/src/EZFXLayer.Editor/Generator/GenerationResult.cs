namespace EZUtils.EZFXLayer
{
    using System.Collections.Generic;

    public class GenerationResult
    {
        public GenerationResult(
            IReadOnlyList<GeneratedClip> generatedClips,
            IReadOnlyList<GeneratedMenu> generatedMenus)
        {
            GeneratedClips = generatedClips;
            GeneratedMenus = generatedMenus;
        }

        public IReadOnlyList<GeneratedClip> GeneratedClips { get; }
        public IReadOnlyList<GeneratedMenu> GeneratedMenus { get; }
    }
}
