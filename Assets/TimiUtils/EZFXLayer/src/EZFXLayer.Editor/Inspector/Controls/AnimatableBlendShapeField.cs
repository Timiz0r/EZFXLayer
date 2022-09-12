namespace EZFXLayer.UIElements
{
    using System;
    using EZFXLayer;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    public class AnimatableBlendShapeField : BindableElement, IRebindable
    {
        private AnimatableBlendShape blendShape;

        public AnimatableBlendShapeField(bool canDelete, AnimatorLayerComponentEditor editor)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimatableBlendShapeField.uxml");
            visualTree.CloneTree(this);

            if (canDelete)
            {
                UnityEngine.UIElements.Button deleteButton = this.Q<UnityEngine.UIElements.Button>();
                deleteButton.RemoveFromClassList("animation-immutable");
                this.Q<UnityEngine.UIElements.Button>().clicked += () => editor.DeleteBlendShape(blendShape);
            }
        }

        public void Rebind(SerializedProperty serializedProperty)
        {
            this.BindProperty(serializedProperty);
            blendShape = Deserialize(serializedProperty);
        }

        public static AnimatableBlendShape Deserialize(SerializedProperty serializedProperty)
        {
            AnimatableBlendShape result = new AnimatableBlendShape()
            {
                skinnedMeshRenderer = (SkinnedMeshRenderer)serializedProperty.FindPropertyRelative("skinnedMeshRenderer").objectReferenceValue,
                name = serializedProperty.FindPropertyRelative("name").stringValue,
                value = serializedProperty.FindPropertyRelative("value").floatValue
            };
            return result;
        }
    }
}
