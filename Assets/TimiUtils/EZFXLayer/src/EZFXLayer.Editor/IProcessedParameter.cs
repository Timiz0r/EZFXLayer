namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    internal interface IProcessedParameter
    {
        VRCExpressionParameters.Parameter ApplyToExpressionParameters(VRCExpressionParameters vrcExpressionParameters);
        void ApplyToControllerParameters(AnimatorController controller);
        AnimatorCondition GetAnimatorCondition(ProcessedAnimation animation);
    }

    internal static class ProcessedParameter
    {
        public static AnimatorControllerParameter GetOrAddParameter(
            List<AnimatorControllerParameter> parameters,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter parameter =
                parameters.FirstOrDefault(p => p.name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (parameter == null)
            {
                parameter = new AnimatorControllerParameter()
                {
                    name = name,
                    //is redundant, since it set it later, but didnt check to see if AddParameter needs this
                    type = type
                };
                parameters.Add(parameter);
            }
            parameter.type = type;
            return parameter;
        }

        public static VRCExpressionParameters.Parameter ApplyToExpressionParameters(
            VRCExpressionParameters vrcExpressionParameters, VRCExpressionParameters.Parameter parameter)
        {
            List<VRCExpressionParameters.Parameter> parameters = new List<VRCExpressionParameters.Parameter>(
                vrcExpressionParameters.parameters);

            VRCExpressionParameters.Parameter targetParameter = parameters.SingleOrDefault(
                p => p.name.Equals(parameter.name, StringComparison.OrdinalIgnoreCase));
            if (targetParameter == null)
            {
                parameters.Add(parameter);
                targetParameter = parameter;
            }
            else
            {
                targetParameter.valueType = parameter.valueType;
                targetParameter.defaultValue = parameter.defaultValue;
            }
            vrcExpressionParameters.parameters = parameters.ToArray();

            return targetParameter;
        }
    }
}
