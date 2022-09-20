namespace EZUtils.EZFXLayer.Test
{
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor.Animations;

    public class GeneratorStateManagementTests
    {
        [Test]
        public void AddsBasicallyEmptyAnimatorLayer_WithEmptyLayerConfig()
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
        public void RemovesUnusedStates_WhenNotPartOfAnimatorLayerConfiguration()
        {
            TestSetup testSetup = new TestSetup();
            AnimatorController controller = testSetup.Assets.FXController;
            controller.AddLayer("foo");
            AnimatorState unusedState = controller.layers[0].stateMachine.AddState("unused");
            _ = testSetup.ConfigurationBuilder.AddLayer("foo");

            testSetup.StandardGenerate();

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

            testSetup.StandardGenerate();

            Assert.That(
                testSetup.Assets.FXController.layers.Select(l => l.name).ToArray(),
                Is.EqualTo(new[] { "1", "2", "3" }));
        }

        [Test]
        public void ReferenceAnimationIsDefaultState()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "layer",
                l => l
                    .ConfigureReferenceAnimation("foo", a => { }));

            testSetup.StandardGenerate();

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

            _ = testSetup.ConfigurationBuilder.AddLayer(
                "layer",
                l => l
                    .AddAnimation("state", a => { }));

            testSetup.StandardGenerate();

            Assert.That(layer.stateMachine.anyStateTransitions[0].exitTime, Is.EqualTo(100f));
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

            testSetup.StandardGenerate();

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
        public void UsesStateName_IfSet()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .ConfigureReferenceAnimation("default", a => a.WithStateName("default2"))
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
                .AddLayer("layer");
            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.FXController.layers[0].stateMachine.states[0].state.writeDefaultValues, Is.False);
        }

        [Test]
        public void EnablesWriteDefaults_WhenOn()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .WriteDefaultValues(enabled: true)
                .AddLayer("layer");
            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.FXController.layers[0].stateMachine.states[0].state.writeDefaultValues, Is.True);
        }
    }
}
