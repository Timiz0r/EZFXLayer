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
        private readonly string menuName;
        private readonly string stateName;
        private readonly AnimationClip animationClip;

        public int Index { get; }
        public bool IsToBeDefaultState { get; }

        public ProcessedAnimation(
            string name,
            string menuName,
            string stateName,
            int index,
            bool isToBeDefaultState,
            AnimationClip animationClip)
        {
            this.name = name;
            this.menuName = menuName;
            this.stateName = stateName;
            Index = index;
            IsToBeDefaultState = isToBeDefaultState;
            this.animationClip = animationClip;
        }

        public bool MatchesState(AnimatorState state)
            => state.name.Equals(stateName, StringComparison.OrdinalIgnoreCase);

        public void AddState(AnimatorController parentAsset, List<AnimatorState> states, ref AnimatorState defaultState)
        {
            AnimatorState correspondingState = states.SingleOrDefault(s => MatchesState(s));
            if (correspondingState != null) return;
            correspondingState = new AnimatorState()
            {
                hideFlags = HideFlags.HideInHierarchy,
                name = stateName
            };
            if (parentAsset != null)
            {
                Utilities.TryAddObjectToAsset(correspondingState, parentAsset);
            }
            states.Add(correspondingState);

            defaultState = IsToBeDefaultState ? correspondingState : defaultState;
        }

        public VRCExpressionsMenu.Control GetMenuItem(VRCExpressionParameters.Parameter parameter)
            => new VRCExpressionsMenu.Control()
            {
                name = menuName,
                parameter = new VRCExpressionsMenu.Control.Parameter() { name = parameter.name },
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                value = Index
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
