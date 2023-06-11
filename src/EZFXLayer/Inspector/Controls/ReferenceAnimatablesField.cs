namespace EZUtils.EZFXLayer.UIElements
{
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    using static Localization;

    public class ReferenceAnimatablesField : BindableElement, ISerializedPropertyContainerItem, IAnimatableConfigurator
    {
        private readonly AnimatorLayerConfigurator configurator;

        private SerializedPropertyContainer blendShapes;
        private SerializedPropertyContainer gameObjects;

        public ReferenceAnimatablesField(AnimatorLayerConfigurator configurator)
        {
            this.configurator = configurator;

            //TODO: on second thought, go with serialized fields since we dont have to hard code paths
            //prob not this, but other controls may move to a common area
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/Controls/ReferenceAnimatablesField.uxml");
            visualTree.CloneTree(this);
            TranslateElementTree(this);

            this.Q<Button>(name: "addBlendShape").clickable.clickedWithEventInfo += evt
                => BlendShapeSelectorPopup.Show(((Button)evt.target).worldBound, this, this.configurator.Scene);
        }

        public void Rebind(SerializedProperty serializedProperty)
        {
            this.BindProperty(serializedProperty);

            VisualElement blendShapeContainer = this.Q<VisualElement>(className: "blend-shape-container");
            blendShapeContainer.Clear(); //BlendShapeContainerRenderer doesn't support reuse
            SerializedProperty blendShapesProperty = serializedProperty.FindPropertyRelative("blendShapes");
            blendShapes?.StopUndoRedoHandling(); //about to make a new one
            blendShapes = new SerializedPropertyContainer(
                blendShapesProperty,
                new BlendShapeContainerRenderer(blendShapeContainer, this));
            blendShapes.Refresh();

            VisualElement gameObjectContainer = this.Q<VisualElement>(className: "gameobject-container");
            SerializedProperty gameObjectsProperty = serializedProperty.FindPropertyRelative("gameObjects");
            gameObjects?.StopUndoRedoHandling();
            gameObjects = SerializedPropertyContainer.CreateSimple(
                gameObjectContainer,
                gameObjectsProperty,
                () => new AnimatableGameObjectField(this));
            gameObjects.Refresh();

#pragma warning disable IDE0001 //purposely spelling out the full object field name because unity has one as well
            _ = this.Q<EZFXLayer.UIElements.ObjectField>(name: "addGameObject").RegisterValueChangedCallback(evt =>
#pragma warning restore IDE0001
            {
                GameObject value = (GameObject)evt.newValue;
#pragma warning disable IDE0001 //purposely spelling out the full object field name because unity has one as well
                ((EZFXLayer.UIElements.ObjectField)evt.target).SetValueWithoutNotify(null);
#pragma warning restore IDE0001

                AddGameObject(new AnimatableGameObject()
                {
                    gameObject = value,
                    path = value.GetRelativePath(),
                    active = false,
                    synchronizeActiveWithReference = true,
                    disabled = false
                });
            });
        }

        public bool IsBlendShapeSelected(SkinnedMeshRenderer smr, string name, out bool permanent, out string key)
        {
            permanent = false;

            AnimatableBlendShape blendShape = blendShapes.AllElements<AnimatableBlendShapeField>()
                .Select(bs => bs.BlendShape)
                .SingleOrDefault(bs => bs.skinnedMeshRenderer == smr && bs.name == name);
            key = blendShape?.key;

            return blendShape != null;
        }

        public void AddBlendShape(AnimatableBlendShape blendShape)
        {
            _ = blendShapes.Add(
                sp => ConfigSerialization.SerializeBlendShape(sp, blendShape),
                applyModifiedProperties: false);

            foreach (AnimationConfigurationField animation in configurator.Animations)
            {
                animation.AddBlendShape(blendShape, applyModifiedProperties: false);
            }

            configurator.ApplyModifiedProperties();
        }

        public void RemoveBlendShape(AnimatableBlendShape blendShape)
        {
            blendShapes.Remove(
                sp => ConfigSerialization.DeserializeBlendShape(sp).Matches(blendShape),
                applyModifiedProperties: false);

            foreach (AnimationConfigurationField animation in configurator.Animations)
            {
                animation.RemoveBlendShape(blendShape, applyModifiedProperties: false);
            }

            //for undo reasons, we've suppressed this call until this point
            configurator.ApplyModifiedProperties();
        }

        public void AddGameObject(AnimatableGameObject gameObject)
        {
            _ = gameObjects.Add(
                sp => ConfigSerialization.SerializeGameObject(sp, gameObject),
                applyModifiedProperties: false);

            foreach (AnimationConfigurationField animation in configurator.Animations)
            {
                animation.AddGameObject(gameObject, applyModifiedProperties: false);
            }

            //for undo reasons, we've suppressed this call until this point
            configurator.ApplyModifiedProperties();
        }

        public void RemoveGameObject(AnimatableGameObject gameObject)
        {
            gameObjects.Remove(
                sp => ConfigSerialization.DeserializeGameObject(sp).Matches(gameObject),
                applyModifiedProperties: false);

            foreach (AnimationConfigurationField element in configurator.Animations)
            {
                element.RemoveGameObject(gameObject, applyModifiedProperties: false);
            }

            //for undo reasons, we've suppressed this call until this point
            configurator.ApplyModifiedProperties();
        }
    }
}
