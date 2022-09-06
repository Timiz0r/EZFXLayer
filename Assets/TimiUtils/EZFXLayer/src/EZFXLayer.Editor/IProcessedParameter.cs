namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;

    public interface IProcessedParameter
    {
        void ApplyToExpressionsParameters();
        void ApplyToControllerParameters(AnimatorController controller);
        void ApplyTransition(
            AnimatorController controller, AnimatorStateMachine stateMachine, ProcessedAnimation animation);
    }

    public static class ProcessedParameter
    {
        public static void ApplyTransition(
            AnimatorController controller,
            AnimatorStateMachine stateMachine,
            ProcessedAnimation animation,
            AnimatorCondition condition)
        {
            List<AnimatorStateTransition> transitions = new List<AnimatorStateTransition>(stateMachine.anyStateTransitions);
            AnimatorState state = animation.CorrespondingState;
            AnimatorStateTransition transition =
                transitions.FirstOrDefault(t => t.destinationState == state);
            if (transition == null)
            {
                transition = new AnimatorStateTransition()
                {
                    hasExitTime = false,
                    hasFixedDuration = true,
                    duration = 0,
                    exitTime = 0,
                    hideFlags = HideFlags.HideInHierarchy,
                    destinationState = state,
                    name = state.name //not sure if name is necessary anyway
                };
                transitions.Add(transition);
                //while we're not creating new assets here, this is okay
                Utilities.TryAddObjectToAsset(transition, controller);
            }
            transition.conditions = new[] { condition };
            stateMachine.anyStateTransitions = transitions.ToArray();
        }

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
    }
}
