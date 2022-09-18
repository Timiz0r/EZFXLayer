namespace EZUtils.EZFXLayer.Test
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;

    public class GeneratorAnimationGenerationTests
    {

        //unlikely to be done on purpose, since it's pointless, but better than failing weirdly
        //or we can decide to throw a useful exception
        [Test]
        public void GeneratesEmptyAnimation_WhenNoAnimatablesConfigured()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "1",
                l => l
                    .ConfigureReferenceAnimation(a => { })
                    .AddAnimation("animation", a => { }));

            testSetup.StandardGenerate();

            //could have combined these two and just looked at states to get clips, but this should be more readable
            Assert.That(
                testSetup.Assets.FXController.layers[0].stateMachine.anyStateTransitions,
                HasCountConstraint.Create(2));
            Assert.That(
                testSetup.Assets.ClipsAdded,
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
                    .ConfigureReferenceAnimation("Off", a => a.AddGameObject(avatarPart, isActive: false))
                    .AddAnimation("On", a => a.SetGameObject(avatarPart, isActive: true))
            );

            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.ClipsAdded, HasCountConstraint.Create(2));

            AnimationClip offClip = testSetup.Assets.ClipsAdded.Single(gc => gc.AnimationName == "Off").Clip;
            AnimationClip onClip = testSetup.Assets.ClipsAdded.Single(gc => gc.AnimationName == "On").Clip;

            Assert.That(GetCurveValue(offClip), Is.False);
            Assert.That(GetCurveValue(onClip), Is.True);

            bool GetCurveValue(AnimationClip clip)
            {
                EditorCurveBinding binding = AnimationUtility.GetCurveBindings(clip).Single();
                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                float value = curve.keys[0].value;
                switch (value)
                {
                    case 0: return false;
                    case 1: return true;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(clip), $"Clip has unexpected curve value of '{value}'.");
                }
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
                    .ConfigureReferenceAnimation("Off", a => a.AddBlendShape(smr, "blendshape", 0f))
                    .AddAnimation("On", a => a.SetBlendShape(smr, "blendshape", 1f))
            );

            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.ClipsAdded, HasCountConstraint.Create(2));

            AnimationClip offClip = testSetup.Assets.ClipsAdded.Single(gc => gc.AnimationName == "Off").Clip;
            AnimationClip onClip = testSetup.Assets.ClipsAdded.Single(gc => gc.AnimationName == "On").Clip;

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

        [Test]
        public void ReturnsOnlyUsedClips_IfStateManagementIsDisabledAndNotAllStatesExist()
        {
            TestSetup testSetup = new TestSetup();
            AnimatorController controller = testSetup.Assets.FXController;
            controller.AddLayer("layer");
            _ = controller.layers[0].stateMachine.AddState("default");
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .DisableStateManagement()
                        .ConfigureReferenceAnimation("default", a => { })
                        .AddAnimation("foo", a => { }));

            testSetup.StandardGenerate();
            Assert.That(testSetup.Assets.ClipsAdded, HasCountConstraint.Create(1));
        }
    }
}
