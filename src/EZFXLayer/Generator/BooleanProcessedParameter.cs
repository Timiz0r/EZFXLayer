namespace EZUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    internal class BooleanProcessedParameter : IProcessedParameter
    {
        private readonly bool defaultValue;
        private readonly bool saved;

        public BooleanProcessedParameter(string name, bool defaultValue, bool saved)
        {
            Name = name;
            this.defaultValue = defaultValue;
            this.saved = saved;
        }
        public string Name { get; }

        public void ApplyToControllerParameters(AnimatorController controller)
        {
            List<AnimatorControllerParameter> parameters =
                new List<AnimatorControllerParameter>(controller.parameters);
            AnimatorControllerParameter parameter = ProcessedParameter.GetOrAddParameter(
                parameters, Name, AnimatorControllerParameterType.Bool);
            parameter.defaultBool = defaultValue;
            controller.parameters = parameters.ToArray();
        }

        public void ApplyToExpressionParameters(VRCExpressionParameters vrcExpressionParameters)
            => ProcessedParameter.ApplyToExpressionParameters(
                vrcExpressionParameters,
                new VRCExpressionParameters.Parameter()
                {
                    name = Name,
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    defaultValue = Convert.ToSingle(defaultValue),
                    saved = saved
                });

        public AnimatorCondition GetAnimatorCondition(int animationToggleValue) => new AnimatorCondition()
        {
            mode = animationToggleValue != 0
                ? AnimatorConditionMode.If //if animator param true
                : AnimatorConditionMode.IfNot,
            parameter = Name
        };
    }
}
