namespace EZFXLayer
{
    using System.Collections.Generic;
    using VRC.SDK3.Avatars.ScriptableObjects;

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
