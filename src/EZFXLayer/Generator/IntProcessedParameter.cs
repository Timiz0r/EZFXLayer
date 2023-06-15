namespace EZUtils.EZFXLayer
{
    using System.Collections.Generic;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    internal class IntProcessedParameter : IProcessedParameter
    {
        private readonly int defaultValue;
        private readonly bool saved;

        public IntProcessedParameter(string name, int defaultValue, bool saved)
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
                parameters, Name, AnimatorControllerParameterType.Int);
            parameter.defaultInt = defaultValue;
            controller.parameters = parameters.ToArray();
        }

        public void ApplyToExpressionParameters(VRCExpressionParameters vrcExpressionParameters)
            => ProcessedParameter.ApplyToExpressionParameters(
                vrcExpressionParameters,
                new VRCExpressionParameters.Parameter()
                {
                    name = Name,
                    valueType = VRCExpressionParameters.ValueType.Int,
                    defaultValue = defaultValue,
                    saved = saved
                });

        public AnimatorCondition GetAnimatorCondition(int animationToggleValue) => new AnimatorCondition()
        {
            mode = AnimatorConditionMode.Equals,
            parameter = Name,
            threshold = animationToggleValue
        };
    }
}
