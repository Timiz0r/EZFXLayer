namespace EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ReferenceComponent))]
    public class ReferenceComponentEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement visualElement = new VisualElement();
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/ReferenceComponentEditor.uxml");
            visualTree.CloneTree(visualElement);

            return visualElement;
        }
    }
}
