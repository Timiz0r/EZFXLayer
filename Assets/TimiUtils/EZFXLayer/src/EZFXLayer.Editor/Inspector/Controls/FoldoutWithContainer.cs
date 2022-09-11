namespace EZFXLayer.UIElements
{
    using UnityEngine.UIElements;
    public class FoldoutWithContainer : Foldout
    {
        private VisualElement separateContainer = null;
        public new class UxmlFactory : UxmlFactory<FoldoutWithContainer, UxmlTraits> { }

        public new class UxmlTraits : Foldout.UxmlTraits { }

        public override VisualElement contentContainer => separateContainer ?? base.contentContainer;

        public void ConfigureSeparateContainer(VisualElement separateContainer)
            => this.separateContainer = separateContainer;
    }
}
