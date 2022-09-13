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
        private AnimatableBlendShape blendShape;
        private bool isFromReferenceAnimation;
        private AnimatableBlendShape referenceBlendShape;
        private readonly AnimatorLayerComponentEditor editor;

        public AnimatableBlendShapeField(AnimatorLayerComponentEditor editor)
        {
            this.editor = editor;

            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimatableBlendShapeField.uxml");
            visualTree.CloneTree(this);

            this.Q<UnityEngine.UIElements.Button>().clicked += () => editor.RemoveBlendShape(blendShape);

            _ = this.Q<Slider>().RegisterValueChangedCallback(evt =>
            {
                blendShape.value = evt.newValue;

                if (isFromReferenceAnimation)
                {
                    editor.ReferenceBlendShapeChanged(blendShape);
                }
                else
                {
                    CheckForReferenceMatch();
                }
            });
        }

        public void Rebind(SerializedProperty serializedProperty, bool isFromReferenceAnimation)
        {
            this.BindProperty(serializedProperty);
            blendShape = Deserialize(serializedProperty);
            this.isFromReferenceAnimation = isFromReferenceAnimation;

            //if we've bound a new reference animation, then assume all the other animations' blendshapes need it
            //(theoretically only a one-time thing, incidentally, not that it matters)
            if (isFromReferenceAnimation)
            {
                editor.ReferenceBlendShapeChanged(blendShape);
            }
        }

        public bool IsElementFor(AnimatableBlendShape blendShape) => this.blendShape.key == blendShape.key;

        public void TryRecordNewReference(AnimatableBlendShape referenceBlendShape)
        {
            if (!IsElementFor(referenceBlendShape)) return;
            this.referenceBlendShape = referenceBlendShape;
            CheckForReferenceMatch();
        }

        private void CheckForReferenceMatch()
            => EnableInClassList(
                "blendshape-matches-reference",
                referenceBlendShape.key == blendShape.key && referenceBlendShape.value == blendShape.value);

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
