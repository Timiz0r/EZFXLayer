namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using UnityEngine;

    //TODO: for ports and adapters reasons, dont make everything public
    internal static class Extension
    {
        internal static GenerationResult Generate(
            this EZFXLayerGenerator generator, IEnumerable<GameObject> avatars, VrcAssets vrcAssets)
            => generator.Generate(
                avatars,
                vrcAssets.FXController,
                vrcAssets.OriginalMenu,
                vrcAssets.Parameters);
    }
}
