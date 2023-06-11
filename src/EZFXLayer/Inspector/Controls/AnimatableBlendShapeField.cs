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
        private readonly IAnimatableConfigurator configurator;

        public AnimatableBlendShape BlendShape { get; }

        public AnimatableBlendShapeField(
            IAnimatableConfigurator configurator,
            SerializedProperty serializedProperty)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/Controls/AnimatableBlendShapeField.uxml");
            visualTree.CloneTree(this);

            this.configurator = configurator;
            this.BindProperty(serializedProperty);
            BlendShape = ConfigSerialization.DeserializeBlendShape(serializedProperty);

            this.Q<Button>().clicked += () => this.configurator.RemoveBlendShape(BlendShape);

            _ = this.Q<Slider>().RegisterValueChangedCallback(evt => BlendShape.value = evt.newValue);
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
