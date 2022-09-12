namespace EZFXLayer.UIElements
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;
    using VRC.SDK3.Avatars.ScriptableObjects;

    [CustomEditor(typeof(AnimatorLayerComponent))]
    public class AnimatorLayerComponentEditor : Editor
    {
        private AnimationConfigurationField referenceField;
        private SerializedPropertyContainer<AnimationConfigurationField> animations;

        public override VisualElement CreateInspectorGUI()
        {
            ViewModelVisualElement visualElement = new ViewModelVisualElement()
            {
                ViewModel = this
            };
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/AnimatorLayerComponentEditor.uxml");
            visualTree.CloneTree(visualElement);

            BindableElement referenceContainer = visualElement.Q<BindableElement>(name: "reference-animation-container");
            referenceField = new AnimationConfigurationField(editor: this, canModify: true);
            referenceField.Rebind(serializedObject.FindProperty("referenceAnimation"));
            referenceContainer.Add(referenceField);

            BindableElement animationContainer = visualElement.Q<BindableElement>(name: "other-animation-container");
            SerializedProperty animationsArray = serializedObject.FindProperty("animations");
            animations = new SerializedPropertyContainer<AnimationConfigurationField>(
                animationContainer, animationsArray, () => new AnimationConfigurationField(editor: this, canModify: true));

            return visualElement;
        }

        public void DeleteBlendShape(AnimatableBlendShape blendShape)
        {
            referenceField.DeleteBlendShape(blendShape);

        }

        public void AddBlendShape(AnimatableBlendShape blendShape)
        {
            referenceField.AddBlendShape(blendShape);
        }
    }
}
