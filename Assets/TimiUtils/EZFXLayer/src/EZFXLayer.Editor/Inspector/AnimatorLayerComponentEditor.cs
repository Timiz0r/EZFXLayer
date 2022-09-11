namespace EZFXLayer.UIElements
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using VRC.SDK3.Avatars.ScriptableObjects;

    [CustomEditor(typeof(AnimatorLayerComponent))]
    public class AnimatorLayerComponentEditor : Editor
    {
        private AnimatorLayerComponent Target => (AnimatorLayerComponent)target;

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
            BindableElement animationContainer = visualElement.Q<BindableElement>(name: "other-animation-container");

            AnimationConfigurationField reference = new AnimationConfigurationField();
            reference.BindProperty(serializedObject.FindProperty("referenceAnimation"));
            referenceContainer.Add(reference);

            return visualElement;
        }
    }
}
