namespace EZUtils.EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEngine.UIElements;
    public class CommonContainer : VisualElement
    {
        private readonly VisualElement container;

        public override VisualElement contentContainer => container;

        private new class UxmlFactory : UxmlFactory<CommonContainer> { }

        public CommonContainer()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/Controls/CommonContainer.uxml");
            visualTree.CloneTree(this);

            container = this.Q<VisualElement>(name: "root-container");
        }
    }
}
