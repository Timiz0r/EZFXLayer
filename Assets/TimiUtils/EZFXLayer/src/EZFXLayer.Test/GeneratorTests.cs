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

        [Test]
        public void Throws_WhenThereAreDuplicateNonEmptyLayers()
        {
            TestSetup testSetup = new TestSetup();
            SkinnedMeshRenderer smr = testSetup.Avatar.AddComponent<SkinnedMeshRenderer>();
            _ = testSetup.ConfigurationBuilder
                .AddLayer("foo",
                    l => l.ConfigureReferenceAnimation(
                        r => r.AddBlendShape(smr, "foo", 1)))
                .AddLayer("foo",
                    l => l.ConfigureReferenceAnimation(
                        r => r.AddBlendShape(smr, "foo", 1)));

            Assert.That(() => testSetup.StandardGenerate(), Throws.InvalidOperationException);
        }
    }
}
