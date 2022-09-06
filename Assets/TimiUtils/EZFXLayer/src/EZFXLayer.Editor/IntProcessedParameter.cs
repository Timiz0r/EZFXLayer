namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.Animations;
    using UnityEngine;

    public class IntProcessedParameter : IProcessedParameter
    {
        private readonly string name;
        private readonly int defaultValue;

        public IntProcessedParameter(string name, int defaultValue)
        {
            this.name = name;
            this.defaultValue = defaultValue;
        }

        public void ApplyToControllerParameters(AnimatorController controller)
        {
            List<AnimatorControllerParameter> parameters =
                new List<AnimatorControllerParameter>(controller.parameters);
            AnimatorControllerParameter parameter = ProcessedParameter.GetOrAddParameter(
                parameters, name, AnimatorControllerParameterType.Int);
            parameter.defaultInt = defaultValue;
            controller.parameters = parameters.ToArray();
        }

        public void ApplyToExpressionsParameters() => throw new NotImplementedException();
        public void ApplyTransition(
            AnimatorController controller,
            AnimatorStateMachine stateMachine,
            ProcessedAnimation animation)
            => ProcessedParameter.ApplyTransition(
                controller,
                stateMachine,
                animation,
                new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.Equals,
                    parameter = name,
                    threshold = animation.Index
                });
    }
}
