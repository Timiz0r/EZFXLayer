namespace EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class VrcAssets
    {
        public AnimatorController OriginalFXController { get; private set; } = new AnimatorController();
        public VRCExpressionsMenu OriginalMenu { get; private set; } = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
        public VRCExpressionParameters OriginalParameters { get; private set; } = ScriptableObject.CreateInstance<VRCExpressionParameters>();

        public AnimatorController FXController { get; private set; } = new AnimatorController();
        public VRCExpressionsMenu Menu { get; private set; } = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
        public VRCExpressionParameters Parameters { get; private set; } = ScriptableObject.CreateInstance<VRCExpressionParameters>();

        public VrcAssets()
        {
            //actually kinda surprised they are null by default 🤷‍♂️
            Parameters.parameters = Array.Empty<VRCExpressionParameters.Parameter>();
            OriginalParameters.parameters = Array.Empty<VRCExpressionParameters.Parameter>();
        }

        public void ResetOriginalMark()
        {
            //would ironically be easier if we used assets, since we could just copy it to deep clone it.
            OriginalFXController = Clone(FXController);
            OriginalMenu = CloneObject(Menu);
            OriginalParameters = CloneObject(Parameters);
        }

        private static AnimatorController Clone(AnimatorController controller)
        {
            AnimatorController newController = new AnimatorController()
            {
                hideFlags = controller.hideFlags,
                name = controller.name,
                parameters = controller.parameters.Select(p => Clone(p)).ToArray()
            };
            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                AnimatorControllerLayer clonedLayer = Clone(layer);
                newController.AddLayer(clonedLayer);
            }
            return newController;
        }

        private static AnimatorControllerParameter Clone(AnimatorControllerParameter parameter) => new AnimatorControllerParameter()
        {
            defaultBool = parameter.defaultBool,
            defaultFloat = parameter.defaultFloat,
            defaultInt = parameter.defaultInt,
            name = parameter.name,
            type = parameter.type
        };

        private static AnimatorControllerLayer Clone(AnimatorControllerLayer layer) => new AnimatorControllerLayer()
        {
            avatarMask = layer.avatarMask,
            blendingMode = layer.blendingMode,
            defaultWeight = layer.defaultWeight,
            iKPass = layer.iKPass,
            name = layer.name,
            stateMachine = Clone(layer.stateMachine),
            syncedLayerAffectsTiming = layer.syncedLayerAffectsTiming,
            syncedLayerIndex = layer.syncedLayerIndex
        };

        private static AnimatorStateMachine Clone(AnimatorStateMachine stateMachine)
        {
            AnimatorStateMachine newStateMachine = new AnimatorStateMachine()
            {
                anyStatePosition = stateMachine.anyStatePosition,
                entryPosition = stateMachine.entryPosition,
                exitPosition = stateMachine.exitPosition,
                hideFlags = stateMachine.hideFlags,
                name = stateMachine.name,
                parentStateMachinePosition = stateMachine.parentStateMachinePosition,
                defaultState = CloneObject(stateMachine.defaultState),
                //no idea if this works, but it's not that important for unit tests here probably
                behaviours = stateMachine.behaviours.Select(b => CloneObject(b)).ToArray()
            };
            //all states should have a unique name (since the gui won't let us get away with non-unique one)
            Dictionary<string, ChildAnimatorState> childStates = stateMachine.states.ToDictionary(
                s => s.state.name,
                s => new ChildAnimatorState()
                {
                    position = s.position,
                    state = CloneObject(s.state)
                });
            Dictionary<string, ChildAnimatorStateMachine> childStateMachines = stateMachine.stateMachines.ToDictionary(
                s => s.stateMachine.name,
                s => new ChildAnimatorStateMachine()
                {
                    position = s.position,
                    stateMachine = Clone(s.stateMachine)
                });
            newStateMachine.states = childStates.Values.ToArray();
            newStateMachine.stateMachines = childStateMachines.Values.ToArray();

            newStateMachine.anyStateTransitions = CloneTransitions(stateMachine.anyStateTransitions);
            newStateMachine.entryTransitions = CloneTransitions(stateMachine.entryTransitions);
            foreach (AnimatorState animatorState in childStates.Values.Select(s => s.state))
            {
                animatorState.transitions = CloneTransitions(animatorState.transitions);
            }
            //note that transitions dont originate from child state machines
            //the child state machine will have its own transitions from
            //states, entry, or anystate, that get handled recursively

            return newStateMachine;

            T[] CloneTransitions<T>(T[] transitions) where T : AnimatorTransitionBase
                => transitions.Select(t => CloneTransition(t)).ToArray();
            T CloneTransition<T>(T transition) where T : AnimatorTransitionBase
            {
                T newTransition = CloneObject(transition);
                newTransition.destinationState =
                    childStates.TryGetValue(transition.destinationState.name, out ChildAnimatorState matchingState)
                    ? matchingState.state
                    : throw new ArgumentOutOfRangeException(
                        nameof(matchingState), $"Somehow couldn't find a matching state.");
                newTransition.destinationStateMachine =
                    childStateMachines.TryGetValue(
                        transition.destinationStateMachine.name, out ChildAnimatorStateMachine matchingStateMachine)
                    ? matchingStateMachine.stateMachine
                    : throw new ArgumentOutOfRangeException(
                        nameof(matchingStateMachine), $"Somehow couldn't find a matching statemachine.");
                return newTransition;
            }
        }
        //TODO: for all this cloning, consider something reflection-based, since there can certainly change with new unity versions
        //perhaps hard with all this logic tho

        private static T CloneObject<T>(T obj) where T : UnityEngine.Object
        {
            T newObj = UnityEngine.Object.Instantiate(obj);
            //desirable in this case
            newObj.name = obj.name;
            return newObj;
        }
    }
}
