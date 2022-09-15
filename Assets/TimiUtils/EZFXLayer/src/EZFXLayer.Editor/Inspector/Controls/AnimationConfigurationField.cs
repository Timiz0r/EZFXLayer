namespace EZFXLayer.UIElements
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal class AnimationConfigurationField : BindableElement, ISerializedPropertyContainerItem
    {
        private readonly ConfigurationOperations configOperations;
        private readonly bool isReferenceAnimation;
        private SerializedPropertyContainer blendShapes;
        private string animationConfigurationKey = null;

        public IEnumerable<AnimatableBlendShapeField> BlendShapes => blendShapes.AllElements<AnimatableBlendShapeField>();

        public AnimationConfigurationField(ConfigurationOperations configOperations, bool isReferenceAnimation)
        {
            this.configOperations = configOperations;
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

            this.Q<UnityEngine.UIElements.Button>(name: "addBlendShape").clickable.clickedWithEventInfo += evt =>
            {
                //TODO: ofc we'll need a picker
                // this.configOperations.AddBlendShape(new AnimatableBlendShape()
                // {
                //     skinnedMeshRenderer = ((AnimatorLayerComponent)this.configOperations.target).gameObject.scene.GetRootGameObjects()[2].GetComponentInChildren<SkinnedMeshRenderer>(),
                //     name = $"florp{blendShapes.Count}",
                //     value = 0
                // });
                configOperations.SelectBlendShapes(buttonBox: ((UnityEngine.UIElements.Button)evt.target).worldBound);
            };

            this.Q<UnityEngine.UIElements.Button>(name: "removeAnimationConfiguration").clicked +=
                () => this.configOperations.RemoveAnimation(animationConfigurationKey);
        }

        public void RemoveBlendShape(AnimatableBlendShape blendShape) => blendShapes.Remove(
            sp => ConfigSerialization.DeserializeBlendShape(sp).Matches(blendShape), apply: false);

        public void AddBlendShape(AnimatableBlendShape blendShape)
            => blendShapes.Add(sp => ConfigSerialization.SerializeBlendShape(sp, blendShape), apply: false);

        public void Rebind(SerializedProperty serializedProperty)
        {
            string newKey =
                serializedProperty.FindPropertyRelative(nameof(AnimationConfiguration.key)).stringValue;
            if (newKey == animationConfigurationKey) return;

            animationConfigurationKey = newKey;

            FoldoutWithContainer foldout = this.Q<FoldoutWithContainer>();
            VisualElement foldoutContent = this.Q<VisualElement>(className: "animation-foldout-content");
            foldout.ConfigureSeparateContainer(foldoutContent);

            //this does need to go below the foldout so that, when we bind to it, it hides the container if needed
            this.BindProperty(serializedProperty);

            VisualElement blendShapeContainer = foldoutContent.Q<VisualElement>(className: "blend-shape-container");
            blendShapeContainer.Clear(); //BlendShapeContainerRenderer doesn't support reuse
            SerializedProperty blendShapesProperty = serializedProperty.FindPropertyRelative("blendShapes");
            blendShapes?.StopUndoRedoHandling(); //about to make a new one
            blendShapes = new SerializedPropertyContainer(
                blendShapesProperty, new BlendShapeContainerRenderer(blendShapeContainer, isReferenceAnimation, configOperations));
            blendShapes.Refresh();

            VisualElement gameObjectContainer = foldoutContent.Q<VisualElement>(className: "gameobject-container");
        }
    }
}
