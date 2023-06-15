namespace EZUtils.EZFXLayer.Test
{
    using System;
    using NUnit.Framework;
    using UnityEngine;

    public class GeneratorTests
    {
        [SetUp]
        public void SetUp() => TestSetup.StandardTestSetUp();

        [Test]
        public void NoChanges_WhenNoLayerConfigs()
        {
            TestSetup testSetup = new TestSetup();
            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());

            generator.Generate(testSetup.Assets);

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
                    l => l.AddInitialAnimation(
                        r => r.SetBlendShape(smr, "foo", 1)))
                .AddLayer("foo",
                    l => l.AddInitialAnimation(
                        r => r.SetBlendShape(smr, "foo", 1)));

            Assert.That(() => testSetup.StandardGenerate(), Throws.InvalidOperationException);
        }

        [Test]
        public void Throws_WhenNoToggleOffAnimations()
        {
            TestSetup testSetup = new TestSetup();
            SkinnedMeshRenderer smr = testSetup.Avatar.AddComponent<SkinnedMeshRenderer>();
            _ = testSetup.ConfigurationBuilder.AddLayer("foo", l => l.AddAnimation("foo", a => a.MakeDefaultAnimation()));

            Assert.That(() => testSetup.StandardGenerate(), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Throws_WhenNoDefaultAnimation()
        {
            TestSetup testSetup = new TestSetup();
            SkinnedMeshRenderer smr = testSetup.Avatar.AddComponent<SkinnedMeshRenderer>();
            _ = testSetup.ConfigurationBuilder.AddLayer("foo", l => l.AddAnimation("foo", a => a.MakeToggleOffAnimation()));

            Assert.That(() => testSetup.StandardGenerate(), Throws.Exception.TypeOf<ArgumentOutOfRangeException>());
        }

        //TODO: tests and design for if existing menus and parameters are encountered
        //TODO: tests and design for if a parameter type will change and there's a layer in the reference with a different name that uses it
    }
}
