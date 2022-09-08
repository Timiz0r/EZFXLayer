namespace EZFXLayer
{
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public interface IAssetRepository
    {
        void FXAnimatorStateMachineAdded(AnimatorStateMachine stateMachine);

        void FXAnimatorControllerStateAdded(AnimatorState animatorState);
        void FXAnimatorControllerStateRemoved(AnimatorState animatorState);

        void FXAnimatorTransitionAdded(AnimatorTransition transition);

        void VRCSubMenuAdded(GeneratedMenu menu);

        void AnimationClipAdded(GeneratedClip clip);
    }
}
