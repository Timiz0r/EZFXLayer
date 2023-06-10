namespace EZUtils.EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    using static Localization;

    internal class AnimationConfigurationField : BindableElement, ISerializedPropertyContainerItem
    {
        private readonly ConfigurationOperations configOperations;
        private string animationConfigurationKey = null;
        private bool isDefaultAnimation;

        public SerializedPropertyContainer BlendShapes { get; private set; }
        public SerializedPropertyContainer GameObjects { get; private set; }

        public AnimationConfigurationField(ConfigurationOperations configOperations)
        {
            this.configOperations = configOperations;

            //TODO: on second thought, go with serialized fields since we dont have to hard code paths
            //prob not this, but other controls may move to a common area
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/Controls/AnimationConfigurationField.uxml");
            visualTree.CloneTree(this);
            TranslateElementTree(this);

            this.Q<Button>(name: "addBlendShape").clickable.clickedWithEventInfo += evt
                => configOperations.SelectBlendShapes(buttonBox: ((Button)evt.target).worldBound, isReference: false);

            this.Q<Button>(name: "removeAnimationConfiguration").clicked += ()
                => this.configOperations.RemoveAnimation(animationConfigurationKey);

            _ = this.Q<TextField>(name: "animationName").RegisterValueChangedCallback(evt =>
            {
                if (isDefaultAnimation)
                {
                    configOperations.PropagateDefaultAnimationNameChangeToDefaultAnimationField();
                }
            });
        }

        public void Rebind(SerializedProperty serializedProperty)
        {
            string newKey =
                serializedProperty.FindPropertyRelative(nameof(AnimationConfiguration.key)).stringValue;
            isDefaultAnimation = serializedProperty.FindPropertyRelative("isDefaultAnimation").boolValue;
            if (newKey == animationConfigurationKey) return;

            animationConfigurationKey = newKey;

            this.BindProperty(serializedProperty);

            FoldoutWithContainer foldout = this.Q<FoldoutWithContainer>();
            VisualElement foldoutContent = this.Q<VisualElement>(className: "animation-foldout-content");
            foldout.ConfigureContainer(foldoutContent);

            VisualElement blendShapeContainer = foldoutContent.Q<VisualElement>(className: "blend-shape-container");
            blendShapeContainer.Clear(); //BlendShapeContainerRenderer doesn't support reuse
            SerializedProperty blendShapesProperty = serializedProperty.FindPropertyRelative("blendShapes");
            BlendShapes?.StopUndoRedoHandling(); //about to make a new one
            BlendShapes = new SerializedPropertyContainer(
                blendShapesProperty,
                new BlendShapeContainerRenderer(blendShapeContainer, configOperations, isReference: false));
            BlendShapes.Refresh();

            VisualElement gameObjectContainer = foldoutContent.Q<VisualElement>(className: "gameobject-container");
            SerializedProperty gameObjectsProperty = serializedProperty.FindPropertyRelative("gameObjects");
            GameObjects?.StopUndoRedoHandling();
            GameObjects = SerializedPropertyContainer.CreateSimple(
                gameObjectContainer,
                gameObjectsProperty,
                () => new AnimatableGameObjectField(configOperations, isReference: false));
            GameObjects.Refresh();

#pragma warning disable IDE0001 //purposely spelling out the full object field name because unity has one as well
            _ = foldoutContent.Q<EZFXLayer.UIElements.ObjectField>(name: "addGameObject").RegisterValueChangedCallback(evt =>
#pragma warning restore IDE0001
            {
                GameObject value = (GameObject)evt.newValue;
#pragma warning disable IDE0001 //purposely spelling out the full object field name because unity has one as well
                ((EZFXLayer.UIElements.ObjectField)evt.target).SetValueWithoutNotify(null);
#pragma warning restore IDE0001
                _ = GameObjects.Add(
                    sp => ConfigSerialization.SerializeGameObject(
                        sp,
                        new AnimatableGameObject()
                        {
                            gameObject = value,
                            path = value.GetRelativePath(),
                            active = false,
                            //since this isn't added from reference propagation,
                            //and, if later added to reference, probably more useful not being synced
                            synchronizeActiveWithReference = false,
                            disabled = false
                        }),
                    apply: false);
            });
        }
    }
}
