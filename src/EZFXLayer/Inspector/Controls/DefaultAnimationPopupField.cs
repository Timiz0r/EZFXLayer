namespace EZUtils.EZFXLayer.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    using static Localization;

    internal class DefaultAnimationPopupField : PopupField<AnimationConfiguration>
    {
        private static readonly AnimationConfiguration nullAnimationConfiguration = new AnimationConfiguration();
        private readonly List<AnimationConfiguration> animations;

        private DefaultAnimationPopupField(List<AnimationConfiguration> animations)
            : base(
                "Default animation",
                animations,
                GetCurrentDefaultAnimation(animations),
                formatSelectedValueCallback: item => animations.Count == 1 && animations[0] == nullAnimationConfiguration
                    ? T("No animations added")
                    : item.name,
                formatListItemCallback: item => animations.Count == 1 && animations[0] == nullAnimationConfiguration
                    ? T("No animations added")
                    : item.name)
        {
            this.animations = animations;

            Undo.undoRedoPerformed += HandleUndoRedo;
            RegisterCallback<DetachFromPanelEvent>(evt => Undo.undoRedoPerformed -= HandleUndoRedo);
        }

        public static DefaultAnimationPopupField Create(List<AnimationConfiguration> animations)
        {
            //we can't get away with just using a ctor
            //in order to handle empty lists, we need to create a copy and potentially altered list
            //that gets used both internally here and internally in PopupField
            animations = animations.Count == 0
                ? new List<AnimationConfiguration>() { nullAnimationConfiguration }
                : animations.ToList();
            return new DefaultAnimationPopupField(animations);
        }

        public void Add(AnimationConfiguration animationConfiguration)
        {
            if (animations.Count == 0 && animations[0] == nullAnimationConfiguration)
            {
                animations.RemoveAt(0);
            }
            animations.Add(animationConfiguration);
        }

        public void Remove(string animationConfigurationKey)
        {
            AnimationConfiguration animationToRemove = animations.Single(a => a.key == animationConfigurationKey);
            _ = animations.Remove(animationToRemove);

            if (animations.Count == 0)
            {
                animations.Add(nullAnimationConfiguration);
            }

            if (animationToRemove.isDefaultAnimation)
            {
                SetValueWithoutNotify(animations[0]);
            }
        }

        public override AnimationConfiguration value
        {
            get => base.value == nullAnimationConfiguration ? null : base.value;
            set
            {
                //this would happen if the user opened the popup and selected nullAnimationConfiguration
                //we dont want this event to propagate and get stored somehow
                if (value == nullAnimationConfiguration) return;
                //and for null, this would happen if the user does it
                base.value = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public override void SetValueWithoutNotify(AnimationConfiguration newValue)
        {
            //unlike value, events dont get propagated, but it seems most sane anyway to stop the underlying field
            //value from getting set in the same way. though it theoretically can't happen, being a private value
            if (newValue == nullAnimationConfiguration) return;
            base.SetValueWithoutNotify(newValue ?? throw new ArgumentNullException(nameof(newValue)));
        }

        private void HandleUndoRedo() => SetValueWithoutNotify(GetCurrentDefaultAnimation(animations));

        private static AnimationConfiguration GetCurrentDefaultAnimation(IEnumerable<AnimationConfiguration> animations)
            => animations.SingleOrDefault(a => a.isDefaultAnimation) ?? animations.First();
    }
}
