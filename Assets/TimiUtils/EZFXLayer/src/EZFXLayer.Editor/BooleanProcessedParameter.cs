namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    internal class BooleanProcessedParameter : IProcessedParameter
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

        public VRCExpressionParameters.Parameter ApplyToExpressionParameters(VRCExpressionParameters vrcExpressionParameters)
            => ProcessedParameter.ApplyToExpressionParameters(
                vrcExpressionParameters,
                new VRCExpressionParameters.Parameter()
                {
                    name = name,
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    defaultValue = Convert.ToSingle(defaultValue),
                    saved = true
                });

        public AnimatorCondition GetAnimatorCondition(ProcessedAnimation animation) => new AnimatorCondition()
        {
            mode = animation.Index != 0
                ? AnimatorConditionMode.If //if animator param true
                : AnimatorConditionMode.IfNot,
            parameter = name
        };
    }
}
