namespace EZUtils.EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEngine.UIElements;

    internal class AnimatableStatus : VisualElement
    {
        private new class UxmlFactory : UxmlFactory<AnimatableStatus, UxmlTraits>
        {
        }

        private new class UxmlTraits : VisualElement.UxmlTraits
        {
        }

        public AnimatableStatus()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/Controls/AnimatableStatus.uxml");
            visualTree.CloneTree(this);
        }
    }
}
