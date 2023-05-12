namespace EZUtils.EZFXLayer.UIElements
{
    using UnityEngine.UIElements;

    //used to be a foldout, but it turns out the foldout, though being bindable, doesnt bind to the underlying toggle
    public class FoldoutWithContainer : Toggle
    {
        private new class UxmlFactory : UxmlFactory<FoldoutWithContainer, UxmlTraits>
        {
        }

        private new class UxmlTraits : Toggle.UxmlTraits
        {
        }

        private VisualElement targetContainer = null;

        public FoldoutWithContainer() : base()
        {
            _ = this.RegisterValueChangedCallback(evt =>
            {
                if (targetContainer == null) return;
                targetContainer.style.display = evt.newValue
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            });

            AddToClassList("unity-foldout__toggle");
        }

        public void ConfigureContainer(VisualElement targetContainer)
        {
            this.targetContainer = targetContainer;
            targetContainer.style.display = value
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}
