namespace EZFXLayer.Test
{
    using System;
    using NUnit.Framework;

    public class Tests
    {
        [Test]
        public void Generate_PerformsNoModifications_WhenConfigurationEmpty()
        {
            VrcAssets vrcAssets = new();
            TestVrcAvatar avatar = new();

            EZFXLayerConfiguration config = new(
                new ReferenceConfiguration(),
                Array.Empty<AnimatorLayerConfiguration>()
            );

            EZFXLayerGenerator generator = new();

            Assert.That(() => generator.Generate(
                config,
                avatar.ToEnumerable(),
                vrcAssets.FXController,
                vrcAssets.Menu,
                vrcAssets.Parameters), Throws.InvalidOperationException);
        }
    }
}
