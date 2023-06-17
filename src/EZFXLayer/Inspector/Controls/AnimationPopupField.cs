namespace EZUtils.EZFXLayer.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    using static Localization;

    internal class AnimationPopupField : PopupField<AnimationConfiguration>
    {
        private static readonly AnimationConfiguration nullAnimationConfiguration =
            AnimationConfiguration.Create("null");
        private readonly List<AnimationConfiguration> animations;
        private readonly Func<AnimationConfiguration, bool> targetAnimationPredicate;

        //we previously had add/remove methods and maintained our own list, kept in sync by the caller
        //but this doesn't work with undo-redo, since the list isn't restored to its past state
        //instead, we let the caller provide the up-to-date list. we pull it so that we can do undo-redo here
        //the alternative is having the caller do undo-redo
        private readonly Func<IReadOnlyList<AnimationConfiguration>> animationRefresher;

        private AnimationPopupField(
            string label,
            List<AnimationConfiguration> animations,
            Func<IReadOnlyList<AnimationConfiguration>> animationRefresher,
            Func<AnimationConfiguration, bool> targetAnimationPredicate)
            : base(
                label,
                animations,
                GetCurrentDefaultAnimation(animations, targetAnimationPredicate),
                formatSelectedValueCallback: item => animations.Count == 1 && animations[0] == nullAnimationConfiguration
                    ? T("No animations added")
                    : item.name,
                formatListItemCallback: item => animations.Count == 1 && animations[0] == nullAnimationConfiguration
                    ? T("No animations added")
                    : item.name)
        {
            //ideally, we'd instead have an empty list shared by base and this, then we refresh
            //first, we can't do that do to C# and the design of the popup class
            //second, we need a usable list to begin with because of how we pass in the default value
            this.animations = animations;
            this.animationRefresher = animationRefresher;
            this.targetAnimationPredicate = targetAnimationPredicate;
            Undo.undoRedoPerformed += HandleUndoRedo;
            RegisterCallback<DetachFromPanelEvent>(evt => Undo.undoRedoPerformed -= HandleUndoRedo);
        }

        public static AnimationPopupField Create(
            string label,
            Func<IReadOnlyList<AnimationConfiguration>> animationRefresher,
            Func<AnimationConfiguration, bool> targetAnimationPredicate)
        {
            //we can't get away with just using a ctor
            //in order to handle empty lists, we need to create a copy and potentially altered list
            //that gets used both internally here and internally in PopupField
            List<AnimationConfiguration> animations = animationRefresher().ToList();
            animations = animations.Count == 0
                ? new List<AnimationConfiguration>() { nullAnimationConfiguration }
                : animations.ToList();
            return new AnimationPopupField(label, animations, animationRefresher, targetAnimationPredicate);
        }

        public void Refresh()
        {
            IReadOnlyList<AnimationConfiguration> newAnimations = animationRefresher();
            animations.Clear();
            animations.AddRange(newAnimations);

            if (animations.Count == 0)
            {
                animations.Add(nullAnimationConfiguration);
                //avoid the event triggering
                SetValueWithoutNotify(nullAnimationConfiguration);
            }
            else
            {
                //used to set without notify, but doing the actual value means we can actually set the field
                //instead of necessitating the code to assume the first animation is the target
                AnimationConfiguration targetAnimation = animations.SingleOrDefault(a => targetAnimationPredicate(a));
                value = targetAnimation ?? animations[0];
            }
        }

        public void RefreshFormat()
        {
            _ = schedule.Execute(() => SetValueWithoutNotify(value ?? nullAnimationConfiguration));
        }

        public override AnimationConfiguration value
        {
            //we generally want to return null instead of nullAnimationConfiguration
            //but popupfield makes a call to value in the ctor, before the format callbacks are set
            //and not after they are set.
            get => base.formatListItemCallback == null || base.value != nullAnimationConfiguration ? base.value : null;
            set
            {
                //this would happen if the user opened the popup and selected nullAnimationConfiguration
                //we dont want this event to propagate
                if (value == nullAnimationConfiguration) return;
                //and for null, this would happen if the user does it
                base.value = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        private void HandleUndoRedo() => Refresh();

        private static AnimationConfiguration GetCurrentDefaultAnimation(
            IEnumerable<AnimationConfiguration> animations,
            Func<AnimationConfiguration, bool> targetAnimationPredicate)
            => animations.SingleOrDefault(targetAnimationPredicate) ?? animations.First();
    }
}
