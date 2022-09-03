namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using NUnit.Framework.Constraints;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using VRC.SDK3.Avatars.Components;

    public class GeneratorTests
    {
        [SetUp]
        public void Init()
        {
            //otherwise the ReferenceConfiguration components add up, and there can only be one
            //TODO: once again reconsider a way to not have to touch components
            GameObject dummy = new GameObject();
            foreach (Object obj in dummy.scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(obj);
            }
        }

        [Test]
        public void Throws_WhenAvatarHasNoDescriptor()
        {
            TestSetup testSetup = new TestSetup();
            Object.DestroyImmediate(testSetup.Avatar.GetComponent<VRCAvatarDescriptor>());
            EZFXLayerGenerator generator = testSetup.CreateGenerator();

            Assert.That(() => generator.Generate(testSetup.Avatars), Throws.InvalidOperationException);
        }

        [Test]
        public void NoChanges_WhenNoLayerConfigs()
        {
            TestSetup testSetup = new TestSetup();
            EZFXLayerGenerator generator = testSetup.CreateGenerator();

            generator.Generate(testSetup.Avatars);

            Assert.That(
                testSetup.Assets.FXController.layers,
                HasCountConstraint.Create(testSetup.Assets.OriginalFXController.layers));
            Assert.That(
                testSetup.Assets.Menu.controls,
                HasCountConstraint.Create(testSetup.Assets.OriginalMenu.controls));
            Assert.That(
                testSetup.Assets.OriginalParameters.parameters,
                HasCountConstraint.Create(testSetup.Assets.OriginalParameters.parameters));
        }

        [Test]
        public void AddsEmptyLayer_WithEmptyLayerConfig()
        {

        }
    }
}
