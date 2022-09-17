namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using UnityEngine;

    //TODO: for ports and adapters reasons, dont make everything public
    internal static class Extension
    {
        internal static void Generate(
            this EZFXLayerGenerator generator, VrcAssets vrcAssets)
            => generator.Generate(
                vrcAssets.FXController,
                vrcAssets.Menu,
                vrcAssets.Parameters);
    }
}
