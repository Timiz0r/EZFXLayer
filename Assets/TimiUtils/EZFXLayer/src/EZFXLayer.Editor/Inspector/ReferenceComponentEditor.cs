namespace EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ReferenceComponent))]
    public class ReferenceComponentEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            ViewModelVisualElement visualElement = new ViewModelVisualElement()
            {
                ViewModel = this
            };
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/ReferenceComponentEditor.uxml");
            visualTree.CloneTree(visualElement);

            // visualElement.Query<Button>().

            return visualElement;
        }
    }
}
