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

            _ = testSetup.StandardGenerate();

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

            _ = testSetup.StandardGenerate();

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

            _ = testSetup.StandardGenerate();

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

            _ = testSetup.StandardGenerate();

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
                    .ConfigureReferenceAnimation(a => { })
                    .AddAnimation("1", a => { }));
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "2",
                l => l
                    .ConfigureReferenceAnimation(a => { })
                    .AddAnimation("1", a => { })
                    .AddAnimation("2", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(
                testSetup.Assets.FXController.parameters[0].type, Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(
                testSetup.Assets.FXController.parameters[1].type, Is.EqualTo(AnimatorControllerParameterType.Int));

            Assert.That(
                testSetup.Assets.Parameters.parameters[0].valueType, Is.EqualTo(VRCExpressionParameters.ValueType.Bool));
            Assert.That(
                testSetup.Assets.Parameters.parameters[1].valueType, Is.EqualTo(VRCExpressionParameters.ValueType.Int));
        }

        [Test]
        public void ReferenceAnimationIsDefaultState()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "layer",
                l => l
                    .ConfigureReferenceAnimation("foo", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.FXController.layers[0].stateMachine.defaultState.name, Is.EqualTo("foo"));
        }

        [Test]
        public void AltersDefaultStateToReferenceAnimation_IfTheOriginalDefaultStateIsDifferent()
        {
            //but will touch conditions
            //also not exhaustively testing all fields because why

            TestSetup testSetup = new TestSetup();

            AnimatorController controller = testSetup.Assets.FXController;
            controller.AddLayer("layer");
            AnimatorControllerLayer layer = controller.layers[0];
            AnimatorState state = layer.stateMachine.AddState("state");
            //is already default, but be explicit
            layer.stateMachine.defaultState = state;
            AnimatorStateTransition transition = layer.stateMachine.AddAnyStateTransition(state);
            transition.exitTime = 100f;

            _ = testSetup.ConfigurationBuilder.AddLayer(
                "layer",
                l => l
                    .ConfigureReferenceAnimation("default", a => { })
                    .AddAnimation("state", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(layer.stateMachine.defaultState.name, Is.EqualTo("default"));
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

            _ = testSetup.StandardGenerate();

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
                    .ConfigureReferenceAnimation(a => { })
                    .AddAnimation("animation", a => { }));

            GenerationResult result = testSetup.StandardGenerate();

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
                    .ConfigureReferenceAnimation("Off", a => a.AddGameObject(avatarPart, isActive: false))
                    .AddAnimation("On", a => a.SetGameObject(avatarPart, isActive: true))
            );

            GenerationResult generationResult = testSetup.StandardGenerate();

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
                    .ConfigureReferenceAnimation("Off", a => a.AddBlendShape(smr, "blendshape", 0f))
                    .AddAnimation("On", a => a.SetBlendShape(smr, "blendshape", 1f))
            );

            GenerationResult generationResult = testSetup.StandardGenerate();

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

        [Test]
        public void DoesNotChangeSavedSetting_IfParameterIsPreExisting()
        {
            TestSetup testSetup = new TestSetup();
            testSetup.Assets.Parameters.parameters = new[]
            {
                new VRCExpressionParameters.Parameter()
                {
                    name = "1",
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    saved = false
                },
                new VRCExpressionParameters.Parameter()
                {
                    name = "2",
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    saved = true
                }
            };
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .AddAnimation("foo", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Parameters.parameters[0].saved, Is.False);
            Assert.That(testSetup.Assets.Parameters.parameters[1].saved, Is.True);
        }

        [Test]
        public void ChangesParameterType_IfNotRightTypeVersusAnimationCount()
        {
            TestSetup testSetup = new TestSetup();

            testSetup.Assets.FXController.AddParameter("1", AnimatorControllerParameterType.Int);
            testSetup.Assets.FXController.AddParameter("2", AnimatorControllerParameterType.Bool);
            testSetup.Assets.Parameters.parameters = new[]
            {
                new VRCExpressionParameters.Parameter()
                {
                    name = "1",
                    valueType = VRCExpressionParameters.ValueType.Int
                },
                new VRCExpressionParameters.Parameter()
                {
                    name = "2",
                    valueType = VRCExpressionParameters.ValueType.Bool
                }
            };

            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(
                testSetup.Assets.FXController.parameters[0].type, Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(
                testSetup.Assets.FXController.parameters[1].type, Is.EqualTo(AnimatorControllerParameterType.Int));

            Assert.That(
                testSetup.Assets.Parameters.parameters[0].valueType, Is.EqualTo(VRCExpressionParameters.ValueType.Bool));
            Assert.That(
                testSetup.Assets.Parameters.parameters[1].valueType, Is.EqualTo(VRCExpressionParameters.ValueType.Int));
        }

        [Test]
        public void ParameterDefaultValue_IsBasedOnIsDefaultAnimation()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .ConfigureReferenceAnimation(a => { })
                        .AddAnimation("foo", a => a.MakeDefaultAnimation()))
                .AddLayer(
                    "2",
                    l => l
                        .ConfigureReferenceAnimation(a => a.MakeDefaultAnimation())
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "3",
                    l => l
                        .ConfigureReferenceAnimation(a => { })
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => a.MakeDefaultAnimation())
                        .AddAnimation("baz", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.FXController.parameters[0].defaultBool, Is.True);
            Assert.That(testSetup.Assets.FXController.parameters[1].defaultBool, Is.False);
            Assert.That(testSetup.Assets.FXController.parameters[2].defaultInt, Is.EqualTo(2));

            Assert.That(testSetup.Assets.Parameters.parameters[0].defaultValue, Is.EqualTo(1f));
            Assert.That(testSetup.Assets.Parameters.parameters[1].defaultValue, Is.EqualTo(0f));
            Assert.That(testSetup.Assets.Parameters.parameters[2].defaultValue, Is.EqualTo(2f));
        }

        [Test]
        public void TransitionConditionsUseRightThresholds_BasedOnOrderingOfAnimation()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .ConfigureReferenceAnimation("default", a => { })
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .ConfigureReferenceAnimation("default", a => { })
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => { })
                        .AddAnimation("baz", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(GetConditionInformation("1", "default").mode, Is.EqualTo(AnimatorConditionMode.IfNot));
            Assert.That(GetConditionInformation("1", "foo").mode, Is.EqualTo(AnimatorConditionMode.If));

            Assert.That(GetConditionInformation("2", "default"), Is.EqualTo((AnimatorConditionMode.Equals, 0f)));
            Assert.That(GetConditionInformation("2", "foo"), Is.EqualTo((AnimatorConditionMode.Equals, 1f)));
            Assert.That(GetConditionInformation("2", "bar"), Is.EqualTo((AnimatorConditionMode.Equals, 2f)));
            Assert.That(GetConditionInformation("2", "baz"), Is.EqualTo((AnimatorConditionMode.Equals, 3f)));

            (AnimatorConditionMode mode, float threshold) GetConditionInformation(string layer, string animation)
            {
                AnimatorCondition condition =
                    testSetup.Assets.FXController.layers.Single(l => l.name == layer)
                        .stateMachine.anyStateTransitions.Single(t => t.destinationState.name == animation)
                            .conditions[0];
                return (condition.mode, condition.threshold);
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

            GenerationResult result = testSetup.StandardGenerate();
            Assert.That(result.GeneratedClips, HasCountConstraint.Create(1));
        }

        [Test]
        public void UsesStateName_IfSet()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .ConfigureReferenceAnimation("default", a => a.WithStateName("default2"))
                        .AddAnimation("foo", a => { }));

            _ = testSetup.StandardGenerate();

            AnimatorState[] states = testSetup.Assets.FXController.layers[0].stateMachine.states
                .Select(s => s.state)
                .ToArray();
            Assert.That(states[0].name, Is.EqualTo("default2"));
            Assert.That(states[1].name, Is.EqualTo("foo"));
        }

        [Test]
        public void UsesRootMenu_IfMenuPathIsNullOrEmptyOrSlash()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath(null)
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath(string.Empty)
                        .AddAnimation("bar", a => { }))
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath("/")
                        .AddAnimation("baz", a => { }));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(result.CreatedSubMenus, HasCountConstraint.Create(0));
            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo("foo"));
            Assert.That(testSetup.Assets.Menu.controls[1].name, Is.EqualTo("bar"));
            Assert.That(testSetup.Assets.Menu.controls[2].name, Is.EqualTo("baz"));
        }

        [Test]
        public void CreatesSubmenus_IfNotPreExisting()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath("1/foo")
                        .AddAnimation("foo", a => { }));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(result.CreatedSubMenus, HasCountConstraint.Create(2));
            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo("1"));
            Assert.That(testSetup.Assets.Menu.controls[0].subMenu.controls[0].name, Is.EqualTo("foo"));
        }

        [Test]
        public void DoesNotCreateSubmenus_IfSlashesAreEscaped()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath(@"\/1"))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath(@"2\/"))
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath(@"/\//3\//\/foo\//\//"));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo("/1"));
            Assert.That(testSetup.Assets.Menu.controls[1].name, Is.EqualTo("2/"));

            Assert.That(testSetup.Assets.Menu.controls[2].name, Is.EqualTo("/"));
            Assert.That(testSetup.Assets.Menu.controls[2].subMenu.controls[0].name, Is.EqualTo("3/"));
            Assert.That(
                testSetup.Assets.Menu.controls[2]
                .subMenu.controls[0]
                .subMenu.controls[0].name, Is.EqualTo("/foo/"));
            Assert.That(
                testSetup.Assets.Menu.controls[2]
                .subMenu.controls[0]
                .subMenu.controls[0]
                .subMenu.controls[0].name, Is.EqualTo("/"));
        }

        [Test]
        public void MenusArePotentiallyCreated_BasedOnBackslashEscaping()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath(@"\\/1"))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath(@"2/\\"))
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath(@"3\\\\/foo"))
                .AddLayer(
                    "4",
                    l => l
                        .WithMenuPath(@"4\\\\\/foo"))
                .AddLayer(
                    "5",
                    l => l
                        .WithMenuPath(@"\foo\bar\")); //aka that backslashes not preceeding a [\\/] work fine

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo(@"\"));
            Assert.That(testSetup.Assets.Menu.controls[0].subMenu.controls[0].name, Is.EqualTo(@"1"));

            Assert.That(testSetup.Assets.Menu.controls[1].name, Is.EqualTo(@"2"));
            Assert.That(testSetup.Assets.Menu.controls[1].subMenu.controls[0].name, Is.EqualTo(@"\"));

            Assert.That(testSetup.Assets.Menu.controls[2].name, Is.EqualTo(@"3\\"));
            Assert.That(testSetup.Assets.Menu.controls[2].subMenu.controls[0].name, Is.EqualTo(@"foo"));

            Assert.That(testSetup.Assets.Menu.controls[3].name, Is.EqualTo(@"4\\/foo"));

            Assert.That(testSetup.Assets.Menu.controls[4].name, Is.EqualTo(@"\foo\bar\"));
        }

        [Test]
        public void SubmenuGenerationWorks_WhenRedundantSlashesArePresent()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath("1/"))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath("/2"))
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath("////////////3////////////////foo/////bar/////"));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo("1"));
            Assert.That(testSetup.Assets.Menu.controls[1].name, Is.EqualTo("2"));
            Assert.That(testSetup.Assets.Menu.controls[2].name, Is.EqualTo("3"));
            Assert.That(
                testSetup.Assets.Menu.controls[2]
                .subMenu.controls[0].name, Is.EqualTo("foo"));
            Assert.That(
                testSetup.Assets.Menu.controls[2]
                .subMenu.controls[0]
                .subMenu.controls[0].name, Is.EqualTo("bar"));
        }

        [Test]
        public void MenuControlNamesCanBeChanged_WhenMenuNameOverrideIsSet()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .AddAnimation("foo", a => a.WithToggleName("bar")));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo("bar"));
        }

        [Test]
        public void NewSubMenusNotCreated_WhenMenuAlreadyExists()
        {
            TestSetup testSetup = new TestSetup();
            testSetup.Assets.Menu.controls.Add(new VRCExpressionsMenu.Control()
            {
                name = "foo",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>()
            });
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .WithMenuPath("foo/bar"));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(result.CreatedSubMenus, HasCountConstraint.Create(1));
            Assert.That(testSetup.Assets.Menu.controls[0].subMenu.controls[0].name, Is.EqualTo("bar"));
        }

        [Test]
        public void NoTogglesCreated_WhenOnlyReferenceAnimation()
        {
            //which is virtually useless, at least at the time of wrinting, having an always-on animation
            //as such, we dont expose a way to change a toggle on the reference animation via configuration builder
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .ConfigureReferenceAnimation("foo", a => { }));

            EZFXLayerConfiguration config = testSetup.ConfigurationBuilder.Generate();
            config.Layers[0].referenceAnimation.toggleNameOverride = "shouldnotexist";
            EZFXLayerGenerator generator = new EZFXLayerGenerator(config);
            GenerationResult result = generator.Generate(testSetup.Avatars, testSetup.Assets);

            Assert.That(testSetup.Assets.Menu.controls, HasCountConstraint.Create(0));
        }
        //need a better result for created submenus that includes path
        //or i mean think about if we can do it via traversal, but prob best to not
    }
}
