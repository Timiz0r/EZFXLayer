namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using NUnit.Framework.Constraints;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using VRC.SDK3.Avatars.Components;

    public class GeneratorTests
    {
        [SetUp]
        public void SetUp() => TestSetup.StandardTestSetUp();

        [Test]
        public void Throws_WhenAvatarHasNoDescriptor()
        {
            TestSetup testSetup = new TestSetup();
            Object.DestroyImmediate(testSetup.Avatar.GetComponent<VRCAvatarDescriptor>());
            EZFXLayerGenerator generator = testSetup.CreateGenerator();

            Assert.That(
                () => _ = generator.Generate(testSetup.Avatars, testSetup.Assets),
                Throws.InvalidOperationException);
        }

        [Test]
        public void NoChanges_WhenNoLayerConfigs()
        {
            TestSetup testSetup = new TestSetup();
            EZFXLayerGenerator generator = testSetup.CreateGenerator();

            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

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
        public void AddsBasicallyEmptyAnimatorLayer_WithEmptyLayerConfig()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer("foo");
            _ = testSetup.ConfigurationBuilder.AddLayer("bar");

            EZFXLayerGenerator generator = testSetup.CreateGenerator();
            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            AnimatorController controller = testSetup.Assets.FXController;
            Assert.That(
                controller.layers.Select(l => l.name),
                Is.EqualTo(new[] { "foo", "bar" }));
        }

        [Test]
        public void AddsNoNewLayer_IfExistingAlready()
        {
            TestSetup testSetup = new TestSetup();
            testSetup.Assets.FXController.AddLayer("foo");
            _ = testSetup.ConfigurationBuilder.AddLayer("foo");

            EZFXLayerGenerator generator = testSetup.CreateGenerator();
            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            Assert.That(
                testSetup.Assets.FXController.layers,
                Has.Exactly(1).Matches<AnimatorControllerLayer>(l => l.name == "foo"));
        }

        [Test]
        public void RemovesUnusedStates_WhenNotPartOfAnimatorLayerConfiguration()
        {
            TestSetup testSetup = new TestSetup();
            AnimatorController controller = testSetup.Assets.FXController;
            controller.AddLayer("foo");
            AnimatorState unusedState = controller.layers[0].stateMachine.AddState("unused");
            _ = testSetup.ConfigurationBuilder.AddLayer("foo");

            EZFXLayerGenerator generator = testSetup.CreateGenerator();
            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            //not a critical assert, but want to verify that RemoveObjectFromAsset doesnt need the state to be a subasset
            //also am not 100% sure that subassets can be determined like this, but that's okay
            //in any case, feel free to delete this if it breaks at some point
            Assert.That(AssetDatabase.GetAssetPath(unusedState), Is.Null.Or.Empty);

            Assert.That(
                controller.layers[0].stateMachine.states,
                Has.None.Matches<ChildAnimatorState>(s => s.state.name == "unused"));
        }

        [Test]
        public void AddsLayerInOrder_BasedOnPreviousLayer()
        {
            TestSetup testSetup = new TestSetup();
            AnimatorController controller = testSetup.Assets.FXController;
            controller.AddLayer("1");
            controller.AddLayer("3");
            _ = testSetup.ConfigurationBuilder.AddLayer("1");
            _ = testSetup.ConfigurationBuilder.AddLayer("2");

            EZFXLayerGenerator generator = testSetup.CreateGenerator();
            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            Assert.That(
                testSetup.Assets.FXController.layers.Select(l => l.name).ToArray(),
                Is.EqualTo(new[] { "1", "2", "3" }));
        }

        //unlikely to be done on purpose, since it's pointless, but better than failing weirdly
        //or we can decide to throw a useful exception
        [Test]
        public void GeneratesEmptyAnimation_WhenNoAnimatablesConfigured()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "1",
                l => l
                    .ConfigureDefaultAnimation(a => { })
                    .AddAnimation("animation", a => { }));

            EZFXLayerGenerator generator = testSetup.CreateGenerator();
            GenerationResult result = generator.Generate(testSetup.Avatars, testSetup.Assets);

            //could have combined these two and just looked at states to get clips, but this should be more readable
            Assert.That(
                testSetup.Assets.FXController.layers[0].stateMachine.anyStateTransitions,
                HasCountConstraint.Create(2));
            Assert.That(
                result.GeneratedClips,
                HasCountConstraint.Create(2).And.All.Matches<GeneratedClip>(gc => gc.Clip.empty));
        }

        [Test]
        public void ChoosesAppropriateParameterType_BasedOnAnimationCount()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "1",
                l => l
                    .ConfigureDefaultAnimation(a => { })
                    .AddAnimation("1", a => { }));
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "2",
                l => l
                    .ConfigureDefaultAnimation(a => { })
                    .AddAnimation("1", a => { })
                    .AddAnimation("2", a => { }));

            EZFXLayerGenerator generator = testSetup.CreateGenerator();
            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            AnimatorControllerParameter[] parameters = testSetup.Assets.FXController.parameters;
            Assert.That(
                testSetup.Assets.FXController.parameters.Select(p => p.type),
                Is.EqualTo(new[] { AnimatorControllerParameterType.Bool, AnimatorControllerParameterType.Int }));
        }
    }
}
