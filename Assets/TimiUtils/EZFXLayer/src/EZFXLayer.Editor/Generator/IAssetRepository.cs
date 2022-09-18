namespace EZFXLayer
{
    using UnityEditor.Animations;

    public interface IAssetRepository
    {
        void FXAnimatorStateMachineAdded(AnimatorStateMachine stateMachine);

        void FXAnimatorControllerStateAdded(AnimatorState animatorState);
        void FXAnimatorControllerStateRemoved(AnimatorState animatorState);

        void FXAnimatorTransitionAdded(AnimatorStateTransition transition);

        void VRCSubMenuAdded(GeneratedMenu menu);

        void AnimationClipAdded(GeneratedClip clip);
    }
}
