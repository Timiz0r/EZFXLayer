namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class AnimationConfiguration
    {
        //TODO: encapsulate stuff more
        public string name;
        public string animatorStateNameOverride;
        public string toggleNameOverride;
        public bool isDefaultState;
        public bool isDefaultAnimation;
        public List<AnimatableBlendShape> blendShapes = new List<AnimatableBlendShape>();

        public List<AnimatableGameObject> gameObjects = new List<AnimatableGameObject>();
        public bool isFoldedOut = true;

        internal bool MatchesState(AnimatorState state)
            => state.name.Equals(EffectiveStateName, StringComparison.OrdinalIgnoreCase);

        internal void AddState(
            List<AnimatorState> states, ref AnimatorState defaultState, IAssetRepository assetRepository)
        {
            AnimatorState correspondingState = states.SingleOrDefault(s => MatchesState(s));
            if (correspondingState != null) return;
            correspondingState = new AnimatorState()
            {
                hideFlags = HideFlags.HideInHierarchy,
                name = EffectiveStateName
            };
            assetRepository.FXAnimatorControllerStateAdded(correspondingState);
            states.Add(correspondingState);

            defaultState = isDefaultState ? correspondingState : defaultState;
        }

        //could hypothetically add these params to ctor, but wont for now
        internal VRCExpressionsMenu.Control GetMenuToggle(string parameterName, float toggleValue)
            => isDefaultState ? null : new VRCExpressionsMenu.Control()
            {
                name = string.IsNullOrEmpty(toggleNameOverride) ? name : toggleNameOverride,
                parameter = new VRCExpressionsMenu.Control.Parameter() { name = parameterName },
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                value = toggleValue
            };

        internal bool SetMotion(AnimatorStateMachine stateMachine, string layerName, out GeneratedClip clip)
        {
            clip = null;
            ChildAnimatorState[] states = stateMachine.states;
            AnimatorState correspondingState = states.SingleOrDefault(s => MatchesState(s.state)).state;
            //would happen if not managing states and the state is not preexisting
            if (correspondingState == null) return false;


            AnimationClip animationClip = GenerateAnimationClip();
            correspondingState.motion = animationClip;
            stateMachine.states = states;

            clip = new GeneratedClip(
                layerName: layerName,
                animationName: name,
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
            => string.IsNullOrEmpty(animatorStateNameOverride) ? name : animatorStateNameOverride;

        private AnimationClip GenerateAnimationClip()
        {
            AnimationClip clip = new AnimationClip();
            float frameRate = clip.frameRate;

            foreach (AnimatableBlendShape blendShape in blendShapes)
            {
                clip.SetCurve(
                    blendShape.skinnedMeshRenderer.gameObject.GetRelativePath(),
                    typeof(SkinnedMeshRenderer),
                    $"blendShape.{blendShape.name}",
                    AnimationCurve.Constant(0, 1f / frameRate, blendShape.value)
                );
            }
            foreach (AnimatableGameObject gameObject in gameObjects)
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
