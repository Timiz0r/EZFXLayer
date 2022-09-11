namespace EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEngine.UIElements;
    public class CommonContainer : VisualElement
    {
        private readonly VisualElement container;

        public override VisualElement contentContainer => container;

        public new class UxmlFactory : UxmlFactory<CommonContainer> { }

        public CommonContainer()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/CommonContainer.uxml");
            visualTree.CloneTree(this);

            container = this.Q<VisualElement>(name: "root-container");
        }
    }
}
