namespace EZFXLayer.UIElements
{
    using System;
    using System.Linq;
    using EZFXLayer;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    public class AnimatableBlendShapeField : BindableElement
    {
        private readonly AnimatorLayerComponentEditor editor;

        public AnimatableBlendShape BlendShape { get; }

        public AnimatableBlendShapeField(
            SerializedProperty serializedProperty, AnimatorLayerComponentEditor editor, bool isFromReferenceAnimation)
        {
            this.BindProperty(serializedProperty);
            BlendShape = Deserialize(serializedProperty);
            this.editor = editor;

            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimatableBlendShapeField.uxml");
            visualTree.CloneTree(this);

            if (isFromReferenceAnimation)
            {
                this.Q<UnityEngine.UIElements.Button>().clicked += () => editor.RemoveBlendShape(BlendShape);
            }

            _ = this.Q<Slider>().RegisterValueChangedCallback(evt =>
            {
                BlendShape.value = evt.newValue;

                if (isFromReferenceAnimation)
                {
                    editor.ReferenceBlendShapeChanged();
                }
                else
                {
                    CheckForReferenceMatch();
                }
            });
            if (!isFromReferenceAnimation)
            {
                CheckForReferenceMatch();
            }
        }

        public void CheckForReferenceMatch()
            => EnableInClassList("blendshape-matches-reference", editor.BlendShapeMatchesReference(BlendShape));

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
            string[] blendShapeNames = lhs.BlendShape.skinnedMeshRenderer.GetBlendShapeNames().ToArray();
            int result =
                Array.IndexOf(blendShapeNames, lhs.BlendShape.name)
                - Array.IndexOf(blendShapeNames, rhs.BlendShape.name);
            return result;
        }
    }
}
