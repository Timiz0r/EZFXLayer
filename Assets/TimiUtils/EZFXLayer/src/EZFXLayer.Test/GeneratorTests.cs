namespace EZFXLayer.Test
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

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
                () => _ = generator.Generate(testSetup.Avatars, testSetup.Assets),
                Throws.InvalidOperationException);
        }

        [Test]
        public void NoChanges_WhenNoLayerConfigs()
        {
            TestSetup testSetup = new TestSetup();
            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());

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

            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());
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

            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());
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

            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());
            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            //not a critical assert, but want to verify that RemoveObjectFromAsset doesnt need the state to be a subasset
            //also am not 100% sure that subassets can be determined like this, but that's okay
            //in any case, feel free to delete this if it breaks at some point
            Assert.That(AssetDatabase.GetAssetPath(unusedState), Is.Null.Or.Empty);

            Assert.That(
                controller.layers[0].stateMachine.states,
                HasCountConstraint.Create(1).And.None.Matches<ChildAnimatorState>(s => s.state.name == "unused"));
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

            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());
            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            Assert.That(
                testSetup.Assets.FXController.layers.Select(l => l.name).ToArray(),
                Is.EqualTo(new[] { "1", "2", "3" }));
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

            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());
            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            AnimatorControllerParameter[] parameters = testSetup.Assets.FXController.parameters;
            Assert.That(
                testSetup.Assets.FXController.parameters.Select(p => p.type),
                Is.EqualTo(new[] { AnimatorControllerParameterType.Bool, AnimatorControllerParameterType.Int }));
        }

        [Test]
        public void DefaultAnimationIsDefaultState()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "layer",
                l => l
                    .ConfigureDefaultAnimation("foo", a => { }));

            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());
            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            Assert.That(testSetup.Assets.FXController.layers[0].stateMachine.defaultState.name, Is.EqualTo("foo"));
        }

        [Test]
        public void DoesNotTouchTransitionFields_WhenTransitionAlreadyExists()
        {
            //but will touch conditions
            //also not exhaustively testing all fields because why

            TestSetup testSetup = new TestSetup();

            AnimatorController controller = testSetup.Assets.FXController;
            controller.AddLayer("layer");
            AnimatorControllerLayer layer = controller.layers[0];
            AnimatorState state = layer.stateMachine.AddState("state");
            AnimatorStateTransition transition = layer.stateMachine.AddAnyStateTransition(state);
            transition.exitTime = 100f;

            _ = testSetup.ConfigurationBuilder.AddLayer(
                "layer",
                l => l
                    .AddAnimation("state", a => { }));

            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());
            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            Assert.That(layer.stateMachine.anyStateTransitions[0].exitTime, Is.EqualTo(100f));
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

            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());
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
        public void GeneratedAnimationsAreCorrect_ForGameObjectAnimations()
        {
            TestSetup testSetup = new TestSetup();
            GameObject avatarPart = new GameObject("Part");
            avatarPart.transform.SetParent(testSetup.Avatar.transform);

            _ = testSetup.ConfigurationBuilder.AddLayer(
                "Part",
                l => l
                    .ConfigureDefaultAnimation("Off", a => a.AddGameObject(avatarPart, isActive: false))
                    .AddAnimation("On", a => a.SetGameObject(avatarPart, isActive: true))
            );

            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());
            GenerationResult generationResult = generator.Generate(testSetup.Avatars, testSetup.Assets);

            AnimationClip offClip = generationResult.GeneratedClips.Single(gc => gc.AnimationName == "Off").Clip;
            AnimationClip onClip = generationResult.GeneratedClips.Single(gc => gc.AnimationName == "On").Clip;

            Assert.That(generationResult.GeneratedClips, HasCountConstraint.Create(2));
            Assert.That(GetCurveValue(offClip), Is.False);
            Assert.That(GetCurveValue(onClip), Is.True);

            bool GetCurveValue(AnimationClip clip)
            {
                EditorCurveBinding binding = AnimationUtility.GetCurveBindings(clip).Single();
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                float value = curve.keys[0].value;
                if (value == 0) return false;
#pragma warning disable IDE0046
                if (value == 1) return true;
#pragma warning restore IDE0046
                throw new ArgumentOutOfRangeException(nameof(clip), $"Clip has unexpected curve value of '{value}'.");
            }
        }

        [Test]
        public void GeneratedAnimationsAreCorrect_ForBlendShapeAnimations()
        {
            TestSetup testSetup = new TestSetup();
            GameObject avatarPart = new GameObject("Part");
            avatarPart.transform.SetParent(testSetup.Avatar.transform);
            SkinnedMeshRenderer smr = avatarPart.AddComponent<SkinnedMeshRenderer>();

            _ = testSetup.ConfigurationBuilder.AddLayer(
                "Part",
                l => l
                    .ConfigureDefaultAnimation("Off", a => a.AddBlendShape(smr, "blendshape", 0f))
                    .AddAnimation("On", a => a.SetBlendShape(smr, "blendshape", 1f))
            );

            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());
            GenerationResult generationResult = generator.Generate(testSetup.Avatars, testSetup.Assets);

            AnimationClip offClip = generationResult.GeneratedClips.Single(gc => gc.AnimationName == "Off").Clip;
            AnimationClip onClip = generationResult.GeneratedClips.Single(gc => gc.AnimationName == "On").Clip;

            Assert.That(generationResult.GeneratedClips, HasCountConstraint.Create(2));
            Assert.That(GetCurveValue(offClip), Is.EqualTo(0f));
            Assert.That(GetCurveValue(onClip), Is.EqualTo(1f));

            float GetCurveValue(AnimationClip clip)
            {
                EditorCurveBinding binding = AnimationUtility.GetCurveBindings(clip).Single();
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                float value = curve.keys[0].value;
                return value;
            }
        }
    }
}
