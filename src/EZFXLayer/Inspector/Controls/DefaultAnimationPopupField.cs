namespace EZUtils.EZFXLayer.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    internal class DefaultAnimationPopupField : PopupField<AnimationConfiguration>
    {
        public List<AnimationConfiguration> Animations { get; }

        public DefaultAnimationPopupField(List<AnimationConfiguration> animations)
            : base(
                "Default animation",
                animations,
                GetCurrentDefaultAnimation(animations),
                formatSelectedValueCallback: ItemFormatter,
                formatListItemCallback: ItemFormatter)
        {
            Animations = animations;

            Undo.undoRedoPerformed += HandleUndoRedo;
            RegisterCallback<DetachFromPanelEvent>(evt => Undo.undoRedoPerformed -= HandleUndoRedo);
        }

        private void HandleUndoRedo() => SetValueWithoutNotify(GetCurrentDefaultAnimation(Animations));

        private static AnimationConfiguration GetCurrentDefaultAnimation(IEnumerable<AnimationConfiguration> animations)
            => animations.SingleOrDefault(a => a.isDefaultAnimation) ?? animations.First();

        private static string ItemFormatter(AnimationConfiguration item) => item.name;
    }
}
