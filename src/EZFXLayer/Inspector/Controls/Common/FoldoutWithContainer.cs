namespace EZUtils.EZFXLayer.UIElements
{
    using UnityEngine.UIElements;
    public class FoldoutWithContainer : Foldout
    {
        private string containerName = null;
        private VisualElement separateContainer = null;
        private new class UxmlFactory : UxmlFactory<FoldoutWithContainer, UxmlTraits> { }

        private new class UxmlTraits : Foldout.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription containerNameAttribute = new UxmlStringAttributeDescription
            {
                name = "containerName",
            };
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((FoldoutWithContainer)ve).containerName = containerNameAttribute.GetValueFromBag(bag, cc);
            }
        }

        public override VisualElement contentContainer => separateContainer ?? base.contentContainer;

        public FoldoutWithContainer()
        {
            //for unknown reasons, this causes the foldout to not open/close correctly initially
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                if (string.IsNullOrEmpty(containerName)) return;

                VisualElement container = panel.visualTree.Q<VisualElement>(name: containerName);
                ConfigureSeparateContainer(container);
            });
        }

        public void ConfigureSeparateContainer(VisualElement separateContainer)
            => this.separateContainer = separateContainer;
    }
}
