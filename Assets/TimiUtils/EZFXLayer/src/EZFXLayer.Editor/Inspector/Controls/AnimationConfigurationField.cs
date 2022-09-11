namespace EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;
    public class AnimationConfigurationField : BindableElement
    {
        public new class UxmlFactory : UxmlFactory<AnimationConfigurationField, UxmlTraits> { }
        public new class UxmlTraits : BindableElement.UxmlTraits { }

        public AnimationConfigurationField()
        {
            //TODO: on second thought, go with serialized fields since we dont have to hard code paths
            //prob not this, but other controls may move to a common area
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimationConfigurationField.uxml");
            visualTree.CloneTree(this);

            // VisualElement foldoutContent = this.Q<VisualElement>(name: "foldoutContent");

            // Toggle foldoutToggle = this.Q<Toggle>(name: "foldoutToggle");
            // _ = foldoutToggle.RegisterValueChangedCallback(e =>
            // {
            //     if (foldoutToggle.value)
            //     {
            //         foldoutContent.RemoveFromClassList("foldout-content-hidden");
            //     }
            //     else
            //     {
            //         foldoutContent.AddToClassList("foldout-content-hidden");
            //     }
            // });
        }
    }
}
