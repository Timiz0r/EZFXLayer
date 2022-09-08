namespace EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class VrcAssets : IAssetRepository
    {
        private readonly List<GeneratedClip> clipsAdded = new List<GeneratedClip>();
        private readonly List<GeneratedMenu> subMenusAdded = new List<GeneratedMenu>();

        public AnimatorController FXController { get; private set; } = new AnimatorController();
        public VRCExpressionsMenu Menu { get; private set; } = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
        public VRCExpressionParameters Parameters { get; private set; } = ScriptableObject.CreateInstance<VRCExpressionParameters>();

        public IReadOnlyList<GeneratedClip> ClipsAdded => clipsAdded;

        public IReadOnlyList<GeneratedMenu> SubMenusAdded => subMenusAdded;

        public VrcAssets()
        {
            //actually kinda surprised they are null by default 🤷‍♂️
            Parameters.parameters = Array.Empty<VRCExpressionParameters.Parameter>();
        }



        public void FXAnimatorStateMachineAdded(AnimatorStateMachine stateMachine) { }
        public void FXAnimatorControllerStateAdded(AnimatorState animatorState) { }
        public void FXAnimatorControllerStateRemoved(AnimatorState animatorState) { }
        public void FXAnimatorTransitionAdded(AnimatorStateTransition transition) { }
        public void VRCSubMenuAdded(GeneratedMenu menu) => subMenusAdded.Add(menu);
        public void AnimationClipAdded(GeneratedClip clip) => clipsAdded.Add(clip);
    }
}
