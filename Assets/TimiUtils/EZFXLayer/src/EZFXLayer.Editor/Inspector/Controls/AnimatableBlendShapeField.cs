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
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimatableBlendShapeField.uxml");
            visualTree.CloneTree(this);

            this.editor = editor;
            this.BindProperty(serializedProperty);
            BlendShape = ConfigSerialization.DeserializeBlendShape(serializedProperty);

            if (!isFromReferenceAnimation)
            {
                CheckForReferenceMatch();
            }

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
        }

        public void CheckForReferenceMatch()
            => EnableInClassList("blendshape-matches-reference", editor.BlendShapeMatchesReference(BlendShape));

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
