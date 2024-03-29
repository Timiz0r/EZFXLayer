namespace EZUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    using static Localization;

    //this class exists because we decided to keep AnimationConfiguration in the components library, which means
    //no referencing UnityEditor. this gives us a data-only class and a basically behavior-only class
    //the alternative to this antipattern is to duplicate the class's structure and copy values over,
    //but that just adds obnoxiousness with no value.
    internal class AnimationConfigurationHelper
    {
        private readonly AnimationConfiguration animation;
        private readonly GenerationOptions generationOptions;

        public AnimationConfigurationHelper(
            AnimationConfiguration animation,
            GenerationOptions generationOptions)
        {
            this.animation = animation;
            this.generationOptions = generationOptions;
        }

        internal bool MatchesState(AnimatorState state)
            => state.name.Equals(EffectiveStateName, StringComparison.OrdinalIgnoreCase);

        internal void AddState(
            List<AnimatorState> states, ref AnimatorState stateMachineDefaultState, IAssetRepository assetRepository)
        {
            AnimatorState correspondingState = states.SingleOrDefault(s => MatchesState(s));
            if (correspondingState != null) return;
            correspondingState = new AnimatorState()
            {
                hideFlags = HideFlags.HideInHierarchy,
                name = EffectiveStateName,
                writeDefaultValues = generationOptions.setWriteDefaultValues
            };
            assetRepository.FXAnimatorControllerStateAdded(correspondingState);
            states.Add(correspondingState);

            //since the expression parameters will "take things over", it kinda doesn't matter if toggle off or default
            //but toggle off is meant to indicate the state with an anystate transition for 0/false
            //and default is meant to indicate, well default, so we use that
            stateMachineDefaultState = animation.isDefaultAnimation ? correspondingState : stateMachineDefaultState;
        }

        //could hypothetically add these params to ctor, but wont for now
        internal VRCExpressionsMenu.Control GetMenuToggle(string parameterName, float parameterValue)
            => animation.isReferenceAnimation ? null : new VRCExpressionsMenu.Control()
            {
                name = string.IsNullOrEmpty(animation.customToggleName)
                    ? animation.name
                    : animation.customToggleName,
                parameter = new VRCExpressionsMenu.Control.Parameter() { name = parameterName },
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                value = parameterValue
            };

        internal bool SetMotion(AnimatorStateMachine stateMachine, string layerName, out GeneratedClip clip)
        {
            clip = null;
            //would happen if not managing states and layer containing the state machine is not preexisting
            if (stateMachine == null)
            {
                Debug.LogWarning(T($"Unable to add animation '{animation.name}' to layer '{layerName}' because the layer does not exist."));
                return false;
            }
            ChildAnimatorState[] states = stateMachine.states;
            AnimatorState correspondingState = states.SingleOrDefault(s => MatchesState(s.state)).state;
            //would happen if not managing states and the state is not preexisting
            if (correspondingState == null)
            {
                Debug.LogWarning(T($"Unable to add animation '{animation.name}' to layer '{layerName}' because a state with a matching name does not exist."));
                return false;
            }


            AnimationClip animationClip = GenerateAnimationClip();
            correspondingState.motion = animationClip;
            stateMachine.states = states;

            clip = new GeneratedClip(
                layerName: layerName,
                animationName: animation.name,
                clip: animationClip
            );
            return true;
        }

        internal void SetTransition(
            AnimatorStateMachine stateMachine,
            AnimatorCondition condition,
            IAssetRepository assetRepository)
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
                assetRepository.FXAnimatorTransitionAdded(transition);
            }
            transition.conditions = new[] { condition };
            stateMachine.anyStateTransitions = transitions.ToArray();
        }

        private string EffectiveStateName
            => string.IsNullOrEmpty(animation.customAnimatorStateName)
                ? animation.name
                : animation.customAnimatorStateName;

        private AnimationClip GenerateAnimationClip()
        {
            AnimationClip clip = new AnimationClip();
            float frameRate = clip.frameRate;

            foreach (AnimatableBlendShape blendShape in animation.blendShapes)
            {
                clip.SetCurve(
                    blendShape.skinnedMeshRenderer.gameObject.GetRelativePath(),
                    typeof(SkinnedMeshRenderer),
                    $"blendShape.{blendShape.name}",
                    AnimationCurve.Constant(0, 1f / frameRate, blendShape.value)
                );
            }
            foreach (AnimatableGameObject gameObject in animation.gameObjects)
            {
                clip.SetCurve(
                    gameObject.gameObject.GetRelativePath(),
                    typeof(GameObject),
                    "m_IsActive",
                    AnimationCurve.Constant(0, 1f / frameRate, gameObject.active ? 1f : 0f)
                );
            }

            return clip;
        }
    }
}
