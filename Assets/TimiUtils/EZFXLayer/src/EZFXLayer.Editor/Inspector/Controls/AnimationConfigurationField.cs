namespace EZFXLayer.UIElements
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    public class AnimationConfigurationField : BindableElement, ISerializedPropertyContainerItem
    {
        private readonly AnimatorLayerComponentEditor editor;
        private readonly bool isReferenceAnimation;
        private SerializedPropertyContainer blendShapes;
        private string animationConfigurationKey = null;

        public IEnumerable<AnimatableBlendShapeField> BlendShapes => blendShapes.AllElements<AnimatableBlendShapeField>();

        public AnimationConfigurationField(AnimatorLayerComponentEditor editor, bool isReferenceAnimation)
        {
            this.editor = editor;
            this.isReferenceAnimation = isReferenceAnimation;

            //TODO: on second thought, go with serialized fields since we dont have to hard code paths
            //prob not this, but other controls may move to a common area
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimationConfigurationField.uxml");
            visualTree.CloneTree(this);

            if (isReferenceAnimation)
            {
                AddToClassList("reference-animation");
            }

            this.Q<UnityEngine.UIElements.Button>(name: "addBlendShape").clicked += () =>
            {
                //TODO: ofc we'll need a picker
                //slightly circular, since we can add here directly, but this keeps it consistent with removes
                editor.AddBlendShape(new AnimatableBlendShape()
                {
                    skinnedMeshRenderer = ((AnimatorLayerComponent)editor.target).gameObject.scene.GetRootGameObjects()[2].GetComponentInChildren<SkinnedMeshRenderer>(),
                    name = $"florp{blendShapes.Count}",
                    value = 0
                });
            };

            this.Q<UnityEngine.UIElements.Button>(name: "removeAnimationConfiguration").clicked += () =>
            {
                editor.RemoveAnimation(animationConfigurationKey);
            };
        }

        public void RemoveBlendShape(AnimatableBlendShape blendShape)
        {
            blendShapes.Remove(sp => AnimatableBlendShapeField.Deserialize(sp).Matches(blendShape), apply: false);
        }

        public void AddBlendShape(AnimatableBlendShape blendShape)
            => blendShapes.Add(sp => AnimatableBlendShapeField.Serialize(sp, blendShape), apply: false);

        public void Rebind(SerializedProperty serializedProperty)
        {
            string newKey =
                serializedProperty.FindPropertyRelative(nameof(AnimationConfiguration.key)).stringValue;
            if (newKey == animationConfigurationKey) return;

            this.BindProperty(serializedProperty);
            animationConfigurationKey = newKey;

            FoldoutWithContainer foldout = this.Q<FoldoutWithContainer>();
            VisualElement foldoutContent = this.Q<VisualElement>(className: "animation-foldout-content");
            foldout.ConfigureSeparateContainer(foldoutContent);

            VisualElement blendShapeContainer = foldoutContent.Q<VisualElement>(className: "blend-shape-container");
            blendShapeContainer.Clear(); //BlendShapeContainerRenderer doesn't support reuse
            SerializedProperty blendShapesProperty = serializedProperty.FindPropertyRelative("blendShapes");
            blendShapes?.StopUndoRedoHandling(); //about to make a new one
            blendShapes = new SerializedPropertyContainer(
                blendShapesProperty, new BlendShapeContainerRenderer(blendShapeContainer, isReferenceAnimation, editor));
            blendShapes.Refresh();

            VisualElement gameObjectContainer = foldoutContent.Q<VisualElement>(className: "gameobject-container");
        }
    }
}
