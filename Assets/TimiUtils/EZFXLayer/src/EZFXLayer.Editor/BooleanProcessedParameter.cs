namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.Animations;
    using UnityEngine;

    public class BooleanProcessedParameter : IProcessedParameter
    {
        private readonly string name;
        private readonly bool defaultValue;

        public BooleanProcessedParameter(string name, bool defaultValue)
        {
            this.name = name;
            this.defaultValue = defaultValue;
        }

        public void ApplyToControllerParameters(AnimatorController controller)
        {
            List<AnimatorControllerParameter> parameters =
                new List<AnimatorControllerParameter>(controller.parameters);
            AnimatorControllerParameter parameter = ProcessedParameter.GetOrAddParameter(
                parameters, name, AnimatorControllerParameterType.Bool);
            parameter.defaultBool = defaultValue;
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
                    mode = animation.Index != 0
                        ? AnimatorConditionMode.If //if animator param true
                        : AnimatorConditionMode.IfNot,
                    parameter = name
                });
    }
}
