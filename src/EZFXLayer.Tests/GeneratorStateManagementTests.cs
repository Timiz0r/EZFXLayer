namespace EZUtils.EZFXLayer.Test
{
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor.Animations;

    public class GeneratorStateManagementTests
    {
        [SetUp]
        public void SetUp() => TestSetup.StandardTestSetUp();

        [Test]
        public void AddsBasicallyEmptyAnimatorLayer_WhenLayerConfigurationEmpty()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer("foo");
            _ = testSetup.ConfigurationBuilder.AddLayer("bar");

            testSetup.StandardGenerate();

            AnimatorController controller = testSetup.Assets.FXController;
            Assert.That(
                controller.layers.Select(l => l.name),
                Is.EqualTo(new[] { "foo", "bar" }));
        }

        [Test]
        public void DoesNotAddAnimatorLayer_WhenNotManagingStates()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer("foo", l => l.DisableStateManagement());
            _ = testSetup.ConfigurationBuilder.AddLayer("bar", l => l.DisableStateManagement());

            testSetup.StandardGenerate();

            AnimatorController controller = testSetup.Assets.FXController;
            Assert.That(
                controller.layers.Select(l => l.name),
                Is.Empty);
        }

        [Test]
        public void AddsNoNewLayer_IfExistingAlready()
        {
            TestSetup testSetup = new TestSetup();
            testSetup.Assets.FXController.AddLayer("foo");
            _ = testSetup.ConfigurationBuilder.AddLayer("foo");

            testSetup.StandardGenerate();

            Assert.That(
                testSetup.Assets.FXController.layers,
                Has.Exactly(1).Matches<AnimatorControllerLayer>(l => l.name == "foo"));
        }

        [Test]
        public void RemovesAllStates_WhenAnimatorLayerConfigurationIsEmpty()
        {
            TestSetup testSetup = new TestSetup();
            AnimatorController controller = testSetup.Assets.FXController;
            controller.AddLayer("foo");
            _ = controller.layers[0].stateMachine.AddState("unused");
            _ = testSetup.ConfigurationBuilder.AddLayer("foo");

            testSetup.StandardGenerate();

            Assert.That(controller.layers[0].stateMachine.states, Is.Empty);
        }

        [Test]
        public void RemovesUnusedStates_WhenNotPartOfAnimatorLayerConfiguration()
        {
            TestSetup testSetup = new TestSetup();
            AnimatorController controller = testSetup.Assets.FXController;
            controller.AddLayer("foo");
            AnimatorState unusedState = controller.layers[0].stateMachine.AddState("unused");
            _ = testSetup.ConfigurationBuilder.AddLayer("foo", l => l.AddInitialAnimation("default", a => { }));

            testSetup.StandardGenerate();

            Assert.That(
                controller.layers[0].stateMachine.states,
                HasCountConstraint.Create(1).And.All.Matches<ChildAnimatorState>(s => s.state.name == "default"));
        }

        [Test]
        public void DoesNotRemovesUnusedStates_WhenStatesNotManaged()
        {
            TestSetup testSetup = new TestSetup();
            AnimatorController controller = testSetup.Assets.FXController;
            controller.AddLayer("foo");
            AnimatorState unusedState = controller.layers[0].stateMachine.AddState("unused");
            _ = testSetup.ConfigurationBuilder.AddLayer("foo", l => l
                .DisableStateManagement()
                .AddInitialAnimation("default", a => { }));

            testSetup.StandardGenerate();

            Assert.That(
                controller.layers[0].stateMachine.states,
                HasCountConstraint.Create(1).And.All.Matches<ChildAnimatorState>(s => s.state.name == "unused"));
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

            testSetup.StandardGenerate();

            Assert.That(
                testSetup.Assets.FXController.layers.Select(l => l.name).ToArray(),
                Is.EqualTo(new[] { "1", "2", "3" }));
        }

        [Test]
        public void DefaultAnimationIsDefaultState()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer1",
                    l => l.AddInitialAnimation("foo", a => { }))
                .AddLayer(
                    "layer2",
                    l => l
                        .AddInitialAnimation("foo", a => a.MakeToggleOffAnimation())
                        .AddAnimation("bar", a => a.MakeDefaultAnimation()));

            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.FXController.layers[0].stateMachine.defaultState.name, Is.EqualTo("foo"));
            Assert.That(testSetup.Assets.FXController.layers[1].stateMachine.defaultState.name, Is.EqualTo("bar"));
        }

        [Test]
        public void AltersStateMachineDefaultStateToDefaultAnimation_IfTheOriginalDefaultStateIsDifferent()
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
                    .AddInitialAnimation("default", a => { })
                    .AddAnimation("state", a => { }));

            testSetup.StandardGenerate();

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
            transition.conditions = new[]
            {
                new AnimatorCondition() { mode = AnimatorConditionMode.Greater, parameter = "idk", threshold = 123f }
            };

            _ = testSetup.ConfigurationBuilder.AddLayer(
                "layer",
                l => l
                    .AddInitialAnimation("state", a => { }));

            testSetup.StandardGenerate();

            Assert.That(layer.stateMachine.anyStateTransitions[0].exitTime, Is.EqualTo(100f));
            Assert.That(layer.stateMachine.anyStateTransitions[0].conditions[0].parameter, Is.Not.EqualTo("idk"));
        }

        [Test]
        public void TransitionConditionsUseRightThresholds_BasedOnOrderingOfAnimationsAndToggleOffAnimation()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                //the first two feature the toggle off animation at 0, so no weird ordering
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath("1")
                        .AddInitialAnimation("default", a => { })
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath("2")
                        .AddInitialAnimation("default", a => { })
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => { })
                        .AddAnimation("baz", a => { }))
                //and these feature the toggle off animation at other points
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath("3")
                        .AddInitialAnimation("default", a => { })
                        .AddAnimation("foo", a => a.MakeToggleOffAnimation()))
                .AddLayer(
                    "4",
                    l => l
                        .WithMenuPath("4")
                        .AddInitialAnimation("default", a => { })
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => a.MakeToggleOffAnimation())
                        .AddAnimation("baz", a => { }))
                .AddLayer(
                    "5",
                    l => l
                        .WithMenuPath("5")
                        .AddInitialAnimation("default", a => { })
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => { })
                        .AddAnimation("baz", a => a.MakeToggleOffAnimation()));

            testSetup.StandardGenerate();

            Assert.That(GetConditionInformation("1", "default").mode, Is.EqualTo(AnimatorConditionMode.IfNot));
            Assert.That(GetConditionInformation("1", "foo").mode, Is.EqualTo(AnimatorConditionMode.If));

            Assert.That(GetConditionInformation("2", "default"), Is.EqualTo((AnimatorConditionMode.Equals, 0f)));
            Assert.That(GetConditionInformation("2", "foo"), Is.EqualTo((AnimatorConditionMode.Equals, 1f)));
            Assert.That(GetConditionInformation("2", "bar"), Is.EqualTo((AnimatorConditionMode.Equals, 2f)));
            Assert.That(GetConditionInformation("2", "baz"), Is.EqualTo((AnimatorConditionMode.Equals, 3f)));

            Assert.That(GetConditionInformation("3", "default").mode, Is.EqualTo(AnimatorConditionMode.If));
            Assert.That(GetConditionInformation("3", "foo").mode, Is.EqualTo(AnimatorConditionMode.IfNot));

            Assert.That(GetConditionInformation("4", "default"), Is.EqualTo((AnimatorConditionMode.Equals, 1f)));
            Assert.That(GetConditionInformation("4", "foo"), Is.EqualTo((AnimatorConditionMode.Equals, 2f)));
            Assert.That(GetConditionInformation("4", "bar"), Is.EqualTo((AnimatorConditionMode.Equals, 0f)));
            Assert.That(GetConditionInformation("4", "baz"), Is.EqualTo((AnimatorConditionMode.Equals, 3f)));

            Assert.That(GetConditionInformation("5", "default"), Is.EqualTo((AnimatorConditionMode.Equals, 1f)));
            Assert.That(GetConditionInformation("5", "foo"), Is.EqualTo((AnimatorConditionMode.Equals, 2f)));
            Assert.That(GetConditionInformation("5", "bar"), Is.EqualTo((AnimatorConditionMode.Equals, 3f)));
            Assert.That(GetConditionInformation("5", "baz"), Is.EqualTo((AnimatorConditionMode.Equals, 0f)));

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
        public void UsesStateName_IfSet()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .AddInitialAnimation("default", a => a.WithStateName("default2"))
                        .AddAnimation("foo", a => { }));

            testSetup.StandardGenerate();

            AnimatorState[] states = testSetup.Assets.FXController.layers[0].stateMachine.states
                .Select(s => s.state)
                .ToArray();
            Assert.That(states[0].name, Is.EqualTo("default2"));
            Assert.That(states[1].name, Is.EqualTo("foo"));
        }

        [Test]
        public void DisablesWriteDefaultValues_WhenOff()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .WriteDefaultValues(enabled: false)
                .AddLayer("layer", l => l.AddInitialAnimation(a => { }));
            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.FXController.layers[0].stateMachine.states[0].state.writeDefaultValues, Is.False);
        }

        [Test]
        public void EnablesWriteDefaults_WhenOn()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .WriteDefaultValues(enabled: true)
                .AddLayer("layer", l => l.AddInitialAnimation(a => { }));
            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.FXController.layers[0].stateMachine.states[0].state.writeDefaultValues, Is.True);
        }
    }
}
