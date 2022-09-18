namespace EZUtils.EZFXLayer.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    internal class DefaultAnimationPopupField : PopupField<AnimationConfiguration>
    {
        private readonly List<AnimationConfiguration> nonReferenceAnimations;
        //including reference as the first
        public List<AnimationConfiguration> AllAnimations { get; }

        //all animations include the reference
        private DefaultAnimationPopupField(
            List<AnimationConfiguration> allAnimations, List<AnimationConfiguration> nonReferenceAnimations)
            : base(
                "Default animation",
                allAnimations,
                GetCurrentDefaultAnimation(allAnimations),
                formatSelectedValueCallback: ItemFormatter,
                formatListItemCallback: ItemFormatter)
        {
            AllAnimations = allAnimations;
            this.nonReferenceAnimations = nonReferenceAnimations;

            Undo.undoRedoPerformed += HandleUndoRedo;
            this.RegisterCallback<DetachFromPanelEvent>(evt => Undo.undoRedoPerformed -= HandleUndoRedo);
        }

        public static DefaultAnimationPopupField Create(
            AnimationConfiguration referenceAnimation,
            List<AnimationConfiguration> animations)
        {
            //we can do most of this just via the ctor, except store the result here for adding and removing
            List<AnimationConfiguration> allAnimations = animations.Prepend(referenceAnimation).ToList();
            DefaultAnimationPopupField result = new DefaultAnimationPopupField(allAnimations, animations);
            return result;
        }

        private void HandleUndoRedo()
        {
            //rather than finding the undid or redid animation, just clear all but the reference animation
            if (nonReferenceAnimations.Count + 1 != AllAnimations.Count)
            {
                AllAnimations.RemoveRange(1, AllAnimations.Count - 1);
                AllAnimations.AddRange(nonReferenceAnimations);
            }
            SetValueWithoutNotify(GetCurrentDefaultAnimation(AllAnimations));
        }

        private static AnimationConfiguration GetCurrentDefaultAnimation(IEnumerable<AnimationConfiguration> animations)
            => animations.SingleOrDefault(a => a.isDefaultAnimation) ?? animations.First();

        private static string ItemFormatter(AnimationConfiguration item) => item.name;
    }
}
