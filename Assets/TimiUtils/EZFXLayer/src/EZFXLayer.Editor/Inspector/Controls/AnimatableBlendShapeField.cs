namespace EZFXLayer.UIElements
{
    using System;
    using System.Linq;
    using EZFXLayer;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    public class AnimatableBlendShapeField : BindableElement, ISerializedPropertyContainerItem
    {
        private AnimatableBlendShape blendShape;
        private readonly AnimatorLayerComponentEditor editor;
        private bool hideIfMatchingReference = false;

        public AnimatableBlendShapeField(AnimatorLayerComponentEditor editor)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimatableBlendShapeField.uxml");
            visualTree.CloneTree(this);

            this.Q<UnityEngine.UIElements.Button>().clicked += () => editor.RemoveBlendShape(blendShape);
            this.editor = editor;
        }

        public void Rebind(SerializedProperty serializedProperty)
        {
            this.BindProperty(serializedProperty);
            blendShape = Deserialize(serializedProperty);
        }

        public bool IsElementFor(AnimatableBlendShape blendShape) => this.blendShape.key == blendShape.key;

        public void SetHideUnchangedItems(bool enabled)
        {
            //TODO: to be called when value in animation changes by looping thru all elements
            hideIfMatchingReference = enabled;
            //TODO: move this to animation. will be part of selector
            this.EnableInClassList("hide-unchanged-items", enabled);
            ApplyMatchingReferenceCheck();
        }

        private void ApplyMatchingReferenceCheck()
        {
            //TODO: to be called when toggle or floatfield are changed. idk if we need two event thingies, prob do
            //TODO: to be called reference (so this class) -> editor -> ApplyMatchingReferenceCheck
        }

        public static AnimatableBlendShape Deserialize(SerializedProperty serializedProperty)
        {
            AnimatableBlendShape result = new AnimatableBlendShape()
            {
                key = serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.key)).stringValue,
                skinnedMeshRenderer = (SkinnedMeshRenderer)serializedProperty.FindPropertyRelative(
                    nameof(AnimatableBlendShape.skinnedMeshRenderer)).objectReferenceValue,
                name = serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.name)).stringValue,
                value = serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.value)).floatValue
            };
            return result;
        }

        public static void Serialize(SerializedProperty serializedProperty, AnimatableBlendShape blendShape)
        {
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.key)).stringValue = blendShape.key;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.skinnedMeshRenderer)).objectReferenceValue =
                blendShape.skinnedMeshRenderer;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.name)).stringValue = blendShape.name;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.value)).floatValue = blendShape.value;
        }

        public static int Compare(AnimatableBlendShapeField lhs, AnimatableBlendShapeField rhs)
        {
            string[] blendShapeNames = lhs.blendShape.skinnedMeshRenderer.GetBlendShapeNames().ToArray();
            int result = Array.IndexOf(blendShapeNames, lhs.name) - Array.IndexOf(blendShapeNames, rhs.name);
            return result;
        }
    }
}
