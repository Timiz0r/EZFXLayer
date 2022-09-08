namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    internal class ProcessedAnimation
    {
        private readonly string name;
        private readonly string toggleName;
        private readonly string stateName;
        private readonly bool isDefaultState;
        private readonly AnimationClip animationClip;
        public readonly bool isDefaultAnimation;

        public ProcessedAnimation(
            string name,
            string toggleName,
            string stateName,
            bool isDefaultState,
            AnimationClip animationClip,
            bool isDefaultAnimation)
        {
            this.name = name;
            this.toggleName = toggleName;
            this.stateName = stateName;
            this.isDefaultState = isDefaultState;
            this.animationClip = animationClip;
            this.isDefaultAnimation = isDefaultAnimation;
        }

        public bool MatchesState(AnimatorState state)
            => state.name.Equals(stateName, StringComparison.OrdinalIgnoreCase);

        public void AddState(
            List<AnimatorState> states, ref AnimatorState defaultState, IAssetRepository assetRepository)
        {
            AnimatorState correspondingState = states.SingleOrDefault(s => MatchesState(s));
            if (correspondingState != null) return;
            correspondingState = new AnimatorState()
            {
                hideFlags = HideFlags.HideInHierarchy,
                name = stateName
            };
            assetRepository.FXAnimatorControllerStateAdded(correspondingState);
            states.Add(correspondingState);

            defaultState = isDefaultState ? correspondingState : defaultState;
        }

        //could hypothetically add these params to ctor, but wont for now
        public VRCExpressionsMenu.Control GetMenuToggle(string parameterName, float toggleValue)
            => isDefaultState ? null : new VRCExpressionsMenu.Control()
            {
                name = toggleName,
                parameter = new VRCExpressionsMenu.Control.Parameter() { name = parameterName },
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                value = toggleValue
            };

        public bool SetMotion(AnimatorStateMachine stateMachine, string layerName, out GeneratedClip clip)
        {
            clip = null;
            ChildAnimatorState[] states = stateMachine.states;
            AnimatorState correspondingState = states.SingleOrDefault(s => MatchesState(s.state)).state;
            //would happen if not managing states and the state is not preexisting
            if (correspondingState == null) return false;

            correspondingState.motion = animationClip;
            stateMachine.states = states;

            clip = new GeneratedClip(
                layerName: layerName,
                animationName: name,
                clip: animationClip
            );
            return true;
        }

        public void SetTransition(
            AnimatorStateMachine stateMachine, AnimatorCondition condition, AnimatorController parentAsset)
        {
            AnimatorState correspondingState = stateMachine.states.SingleOrDefault(s => MatchesState(s.state)).state;
            if (correspondingState == null) return;

            List<AnimatorStateTransition> transitions = new List<AnimatorStateTransition>(stateMachine.anyStateTransitions);
            AnimatorStateTransition transition =
                transitions.FirstOrDefault(t => t.destinationState == correspondingState);
            if (transition == null)
            {
                transition = new AnimatorStateTransition()
                {
                    hasExitTime = false,
                    hasFixedDuration = true,
                    duration = 0,
                    exitTime = 0,
                    hideFlags = HideFlags.HideInHierarchy,
                    destinationState = correspondingState,
                    name = correspondingState.name //not sure if name is necessary anyway
                };
                transitions.Add(transition);
                //while we're not creating new assets here, this is okay
                Utilities.TryAddObjectToAsset(transition, parentAsset);
            }
            transition.conditions = new[] { condition };
            stateMachine.anyStateTransitions = transitions.ToArray();
        }
    }
}
