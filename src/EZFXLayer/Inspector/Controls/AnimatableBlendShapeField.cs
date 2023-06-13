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

            VisualElement fieldContainer = this.Q<VisualElement>(className: "animatable-field-container");
            Toggle disabledToggle = this.Q<Toggle>(name: "disabled");
            _ = disabledToggle.RegisterValueChangedCallback(
                evt => fieldContainer.EnableInClassList("animatable-disabled", evt.newValue));
            _ = schedule.Execute(() => fieldContainer.EnableInClassList("animatable-disabled", disabledToggle.value));

            this.Q<Button>(name: "remove").clicked += () => this.configurator.RemoveBlendShape(BlendShape);
            //note that this button will be hidden unless disabled
            //and whether or not disabled is able to be set is controlled by ref and anim fields
            this.Q<Button>(name: "add").clicked += () => this.configurator.AddBlendShape(BlendShape);

            //we do this because BlendShape is public and will be read; AnimatableBlendShape is only serializable, not an Object
            //we only need to do it for one of the slider or field, not both, since they get updated in unison via binding
            _ = this.Q<Slider>(name: "valueSlider").RegisterValueChangedCallback(evt => BlendShape.value = evt.newValue);
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
