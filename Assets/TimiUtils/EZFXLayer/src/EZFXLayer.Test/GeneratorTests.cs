namespace EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class GeneratorTests
    {
        [SetUp]
        public void SetUp() => TestSetup.StandardTestSetUp();

        [Test]
        public void Throws_WhenAvatarHasNoDescriptor()
        {
            TestSetup testSetup = new TestSetup();
            UnityEngine.Object.DestroyImmediate(testSetup.Avatar.GetComponent<VRCAvatarDescriptor>());
            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());

            Assert.That(
                () => generator.Generate(testSetup.Avatars, testSetup.Assets),
                Throws.InvalidOperationException);
        }

        [Test]
        public void NoChanges_WhenNoLayerConfigs()
        {
            TestSetup testSetup = new TestSetup();
            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());

            generator.Generate(testSetup.Avatars, testSetup.Assets);

            Assert.That(
                testSetup.Assets.FXController.layers,
                HasCountConstraint.Create(0));
            Assert.That(
                testSetup.Assets.Menu.controls,
                HasCountConstraint.Create(0));
            Assert.That(
                testSetup.Assets.Parameters.parameters,
                HasCountConstraint.Create(0));
        }

        //TODO: tests around duplicate layers. we'll allow them for empty layers, tho, useful as markers.
        //well, duplicate layers where one is a marker isn't too useful yet tho has it's rare scenario.
        //for avatar-specific settings eventually, this may be important depending on how we implement that.
    }
}
