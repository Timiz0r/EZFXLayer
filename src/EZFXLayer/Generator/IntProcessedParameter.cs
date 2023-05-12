namespace EZUtils.EZFXLayer
{
    using System.Collections.Generic;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    internal class IntProcessedParameter : IProcessedParameter
    {
        private readonly string name;
        private readonly int defaultValue;
        private readonly bool saved;

        public IntProcessedParameter(string name, int defaultValue, bool saved)
        {
            this.name = name;
            this.defaultValue = defaultValue;
            this.saved = saved;
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
                    saved = saved
                });

        public AnimatorCondition GetAnimatorCondition(int animationIndex) => new AnimatorCondition()
        {
            mode = AnimatorConditionMode.Equals,
            parameter = name,
            threshold = animationIndex
        };
    }
}
