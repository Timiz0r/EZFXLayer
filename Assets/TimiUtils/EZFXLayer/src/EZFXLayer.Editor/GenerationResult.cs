namespace EZFXLayer
{
    using System.Collections.Generic;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class GenerationResult
    {
        public GenerationResult(
            IReadOnlyList<GeneratedClip> generatedClips,
            IReadOnlyList<VRCExpressionsMenu> createdSubMenus)
        {
            GeneratedClips = generatedClips;
            CreatedSubMenus = createdSubMenus;
        }

        public IReadOnlyList<GeneratedClip> GeneratedClips { get; }
        public IReadOnlyList<VRCExpressionsMenu> CreatedSubMenus { get; }
    }
}
