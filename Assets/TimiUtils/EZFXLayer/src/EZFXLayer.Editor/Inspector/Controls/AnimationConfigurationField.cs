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
        private string animationConfigurationKey = null;

        private SerializedPropertyContainer blendShapes;
        private SerializedPropertyContainer gameObjects;

        public IEnumerable<AnimatableBlendShapeField> BlendShapes => blendShapes.AllElements<AnimatableBlendShapeField>();
        public IEnumerable<AnimatableGameObjectField> GameObjects => gameObjects.AllElements<AnimatableGameObjectField>();

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

            this.Q<UnityEngine.UIElements.Button>(name: "addBlendShape").clickable.clickedWithEventInfo += evt
                => configOperations.SelectBlendShapes(buttonBox: ((UnityEngine.UIElements.Button)evt.target).worldBound);

            this.Q<UnityEngine.UIElements.Button>(name: "removeAnimationConfiguration").clicked += ()
                => this.configOperations.RemoveAnimation(animationConfigurationKey);
        }

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
                blendShapesProperty,
                new BlendShapeContainerRenderer(blendShapeContainer, isReferenceAnimation, configOperations));
            blendShapes.Refresh();

            VisualElement gameObjectContainer = foldoutContent.Q<VisualElement>(className: "gameobject-container");
            SerializedProperty gameObjectsProperty = serializedProperty.FindPropertyRelative("gameObjects");
            gameObjects?.StopUndoRedoHandling();
            gameObjects = SerializedPropertyContainer.CreateSimple(
                gameObjectContainer,
                gameObjectsProperty,
                () => new AnimatableGameObjectField(configOperations, isReferenceAnimation));
            gameObjects.Refresh();

            _ = foldoutContent.Q<EZFXLayer.UIElements.ObjectField>(name: "addGameObject").RegisterValueChangedCallback(evt =>
            {
                GameObject value = (GameObject)evt.newValue;
                ((EZFXLayer.UIElements.ObjectField)evt.target).SetValueWithoutNotify(null);

                configOperations.AddGameObject(new AnimatableGameObject()
                {
                    gameObject = value,
                    path = value.GetRelativePath(),
                    active = false
                });
            });
        }

        public void RemoveBlendShape(AnimatableBlendShape blendShape) => blendShapes.Remove(
            sp => ConfigSerialization.DeserializeBlendShape(sp).Matches(blendShape), apply: false);

        public void AddBlendShape(AnimatableBlendShape blendShape)
            => blendShapes.Add(sp => ConfigSerialization.SerializeBlendShape(sp, blendShape), apply: false);

        public void RemoveGameObject(AnimatableGameObject gameObject) => gameObjects.Remove(
            sp => ConfigSerialization.DeserializeGameObject(sp).Matches(gameObject), apply: false);

        public void AddGameObject(AnimatableGameObject gameObject)
            => gameObjects.Add(sp => ConfigSerialization.SerializeGameObject(sp, gameObject), apply: false);

        public bool IsMatch(AnimationConfiguration animation) => animation.key == animationConfigurationKey;
    }
}
