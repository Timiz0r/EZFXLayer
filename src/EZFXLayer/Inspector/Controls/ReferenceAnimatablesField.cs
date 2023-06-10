namespace EZUtils.EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    using static Localization;

    internal class ReferenceAnimatablesField : BindableElement, ISerializedPropertyContainerItem
    {
        private readonly ConfigurationOperations configOperations;

        public SerializedPropertyContainer BlendShapes { get; private set; }
        public SerializedPropertyContainer GameObjects { get; private set; }

        public ReferenceAnimatablesField(ConfigurationOperations configOperations)
        {
            this.configOperations = configOperations;

            //TODO: on second thought, go with serialized fields since we dont have to hard code paths
            //prob not this, but other controls may move to a common area
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/Controls/ReferenceAnimatablesField.uxml");
            visualTree.CloneTree(this);
            TranslateElementTree(this);

            this.Q<Button>(name: "addBlendShape").clickable.clickedWithEventInfo += evt
                => configOperations.SelectBlendShapes(buttonBox: ((Button)evt.target).worldBound, isReference: true);
        }

        public void Rebind(SerializedProperty serializedProperty)
        {
            this.BindProperty(serializedProperty);

            VisualElement blendShapeContainer = this.Q<VisualElement>(className: "blend-shape-container");
            blendShapeContainer.Clear(); //BlendShapeContainerRenderer doesn't support reuse
            SerializedProperty blendShapesProperty = serializedProperty.FindPropertyRelative("blendShapes");
            BlendShapes?.StopUndoRedoHandling(); //about to make a new one
            BlendShapes = new SerializedPropertyContainer(
                blendShapesProperty,
                new BlendShapeContainerRenderer(blendShapeContainer, configOperations, isReference: true));
            BlendShapes.Refresh();

            VisualElement gameObjectContainer = this.Q<VisualElement>(className: "gameobject-container");
            SerializedProperty gameObjectsProperty = serializedProperty.FindPropertyRelative("gameObjects");
            GameObjects?.StopUndoRedoHandling();
            GameObjects = SerializedPropertyContainer.CreateSimple(
                gameObjectContainer,
                gameObjectsProperty,
                () => new AnimatableGameObjectField(configOperations, isReference: true));
            GameObjects.Refresh();

#pragma warning disable IDE0001 //purposely spelling out the full object field name because unity has one as well
            _ = this.Q<EZFXLayer.UIElements.ObjectField>(name: "addGameObject").RegisterValueChangedCallback(evt =>
#pragma warning restore IDE0001
            {
                GameObject value = (GameObject)evt.newValue;
#pragma warning disable IDE0001 //purposely spelling out the full object field name because unity has one as well
                ((EZFXLayer.UIElements.ObjectField)evt.target).SetValueWithoutNotify(null);
#pragma warning restore IDE0001

                configOperations.AddReferenceGameObject(new AnimatableGameObject()
                {
                    gameObject = value,
                    path = value.GetRelativePath(),
                    active = false,
                    synchronizeActiveWithReference = true,
                    disabled = false
                });
            });
        }
    }
}
