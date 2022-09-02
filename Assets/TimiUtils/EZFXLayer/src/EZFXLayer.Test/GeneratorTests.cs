namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    public class GeneratorTests
    {
        [Test]
        public void Throws_WhenAvatarHasNoDescriptor()
        {
            TestSetup testSetup = new TestSetup();
            Object.DestroyImmediate(testSetup.Avatar.GetComponent<VRCAvatarDescriptor>());
            EZFXLayerGenerator generator = testSetup.CreateGenerator();

            Assert.That(() => generator.Generate(testSetup.Avatars), Throws.InvalidOperationException);
        }

        [Test]
        public void CreatesAssets_WithNoAnimatorLayersAndNoPreexistingGenerations()
        {
            TestSetup testSetup = new TestSetup();
            EZFXLayerGenerator generator = testSetup.CreateGenerator();

            generator.Generate(testSetup.Avatars);


        }

        [Test]
        public void CreatesGitIgnoreFile()
        {

        }
    }
}
