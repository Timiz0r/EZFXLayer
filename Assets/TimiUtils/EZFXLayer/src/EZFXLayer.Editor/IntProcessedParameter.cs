namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    internal class IntProcessedParameter : IProcessedParameter
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

        public VRCExpressionParameters.Parameter ApplyToExpressionParameters(VRCExpressionParameters vrcExpressionParameters)
            => ProcessedParameter.ApplyToExpressionParameters(
                vrcExpressionParameters,
                new VRCExpressionParameters.Parameter()
                {
                    name = name,
                    valueType = VRCExpressionParameters.ValueType.Int,
                    defaultValue = defaultValue,
                    saved = true
                });

        public AnimatorCondition GetAnimatorCondition(ProcessedAnimation animation) => new AnimatorCondition()
        {
            mode = AnimatorConditionMode.Equals,
            parameter = name,
            threshold = animation.Index
        };
    }
}
