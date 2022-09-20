namespace EZUtils.EZFXLayer.UIElements
{
    using System;
    using System.Linq;
    using EZFXLayer;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    internal class AnimatableBlendShapeField : BindableElement
    {
        private readonly ConfigurationOperations configOperations;

        public AnimatableBlendShape BlendShape { get; }

        public AnimatableBlendShapeField(
            ConfigurationOperations configOperations,
            SerializedProperty serializedProperty,
            bool isFromReferenceAnimation)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/src/EZFXLayer.Editor/Inspector/Controls/AnimatableBlendShapeField.uxml");
            visualTree.CloneTree(this);

            this.configOperations = configOperations;
            this.BindProperty(serializedProperty);
            BlendShape = ConfigSerialization.DeserializeBlendShape(serializedProperty);

            if (!isFromReferenceAnimation)
            {
                CheckForReferenceMatch();
            }

            if (isFromReferenceAnimation)
            {
                this.Q<Button>().clicked += () => this.configOperations.RemoveBlendShape(BlendShape);
            }

            _ = this.Q<Slider>().RegisterValueChangedCallback(evt =>
            {
                BlendShape.value = evt.newValue;

                if (isFromReferenceAnimation)
                {
                    this.configOperations.ReferenceBlendShapeChanged();
                }
                else
                {
                    CheckForReferenceMatch();
                }
            });
        }

        public void CheckForReferenceMatch()
            => EnableInClassList("blendshape-matches-reference", configOperations.BlendShapeMatchesReference(BlendShape));

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
