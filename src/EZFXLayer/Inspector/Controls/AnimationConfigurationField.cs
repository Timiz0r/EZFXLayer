namespace EZUtils.EZFXLayer.UIElements
{
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    using static Localization;

    public class AnimationConfigurationField : BindableElement, ISerializedPropertyContainerItem, IAnimatableConfigurator
    {
        private readonly AnimatorLayerConfigurator configurator;
        private string animationConfigurationKey = null;

        private SerializedPropertyContainer blendShapes;
        private SerializedPropertyContainer gameObjects;

        public AnimationConfigurationField(AnimatorLayerConfigurator configurator)
        {
            this.configurator = configurator;

            //TODO: on second thought, go with serialized fields since we dont have to hard code paths
            //prob not this, but other controls may move to a common area
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/Controls/AnimationConfigurationField.uxml");
            visualTree.CloneTree(this);
            TranslateElementTree(this);

            this.Q<Button>(name: "addBlendShape").clickable.clickedWithEventInfo += evt
                => BlendShapeSelectorPopup.Show(((Button)evt.target).worldBound, this, this.configurator.Scene);

            this.Q<Button>(name: "removeAnimationConfiguration").clicked += ()
                => this.configurator.RemoveAnimation(animationConfigurationKey);

            Toggle isDefaultAnimationToggle = this.Q<Toggle>(name: "isDefaultAnimation");
            _ = this.Q<TextField>(name: "animationName").RegisterValueChangedCallback(evt =>
            {
                if (isDefaultAnimationToggle.value)
                {
                    configurator.PropagateAnimationNameChangeToPopups();
                }
            });
        }

        public void Rebind(SerializedProperty serializedProperty)
        {
            string newKey =
                serializedProperty.FindPropertyRelative(nameof(AnimationConfiguration.key)).stringValue;
            if (newKey == animationConfigurationKey) return;

            animationConfigurationKey = newKey;

            this.BindProperty(serializedProperty);

            FoldoutWithContainer foldout = this.Q<FoldoutWithContainer>();
            VisualElement foldoutContent = this.Q<VisualElement>(className: "animation-foldout-content");
            foldout.ConfigureContainer(foldoutContent);

            VisualElement blendShapeContainer = foldoutContent.Q<VisualElement>(className: "blend-shape-container");
            blendShapeContainer.Clear(); //BlendShapeContainerRenderer doesn't support reuse
            SerializedProperty blendShapesProperty = serializedProperty.FindPropertyRelative("blendShapes");
            blendShapes?.StopUndoRedoHandling(); //about to make a new one
            blendShapes = new SerializedPropertyContainer(
                blendShapesProperty,
                new BlendShapeContainerRenderer(blendShapeContainer, this));
            blendShapes.Refresh();

            VisualElement gameObjectContainer = foldoutContent.Q<VisualElement>(className: "gameobject-container");
            SerializedProperty gameObjectsProperty = serializedProperty.FindPropertyRelative("gameObjects");
            gameObjects?.StopUndoRedoHandling();
            gameObjects = SerializedPropertyContainer.CreateSimple(
                gameObjectContainer,
                gameObjectsProperty,
                () => new AnimatableGameObjectField(this));
            gameObjects.Refresh();

#pragma warning disable IDE0001 //purposely spelling out the full object field name because unity has one as well
            _ = foldoutContent.Q<EZFXLayer.UIElements.ObjectField>(name: "addGameObject").RegisterValueChangedCallback(evt =>
#pragma warning restore IDE0001
            {
                GameObject value = (GameObject)evt.newValue;
#pragma warning disable IDE0001 //purposely spelling out the full object field name because unity has one as well
                ((EZFXLayer.UIElements.ObjectField)evt.target).SetValueWithoutNotify(null);
#pragma warning restore IDE0001
                AddGameObject(new AnimatableGameObject(
                    gameObject: value,
                    path: value.GetRelativePath(),
                    active: false,
                    //note that this is added to animation, not reference
                    //in the event the same is later added to reference, the most desired behavior would be
                    //to maintain the existing value and not sync
                    //while it would be hypothetically desirable to sync if the value matches, the desired
                    //reference value is set after being added, so we should not do this automatically
                    synchronizeActiveWithReference: false,
                    disabled: false));
            });
        }

        public bool IsBlendShapeSelected(
            SkinnedMeshRenderer smr, string name, out bool permanent, out AnimatableBlendShape existing)
        {
            existing = blendShapes.AllElements<AnimatableBlendShapeField>()
                .Select(bs => bs.BlendShape)
                .SingleOrDefault(bs => bs.skinnedMeshRenderer == smr && bs.name == name);
            //note that we should be ensuring key consistency elsewhere, in the case that we add to the reference
            //a blend shape that already exists in the animation
            permanent = configurator.Reference.IsBlendShapeSelected(
                smr, name, out _, out AnimatableBlendShape refExisting)
                && existing != null
                && existing.key == refExisting?.key;

            return existing != null;
        }
        //if blendshapes added to animation, we dont want them synced up if reference ever gets the same
        public bool DefaultSynchronizeValueWithReference() => false;

        public void AddBlendShape(AnimatableBlendShape blendShape)
            => AddBlendShape(blendShape, applyModifiedProperties: true);
        public void RemoveBlendShape(AnimatableBlendShape blendShape)
            => RemoveBlendShape(blendShape, applyModifiedProperties: true);
        public void AddGameObject(AnimatableGameObject gameObject)
            => AddGameObject(gameObject, applyModifiedProperties: true);
        public void RemoveGameObject(AnimatableGameObject gameObject)
            => RemoveGameObject(gameObject, applyModifiedProperties: true);

        //these exist because reference needs a way to not apply modified properties, since it's doing a batch operation
        //the above methods are meant for adding animatables specifically to a single animation and therefore can apply
        //
        //when adding an animatable that already exists...
        //we don't want duplicates
        //to support reference animatables being added after animation animatables, we want consistent keys
        internal void AddBlendShape(AnimatableBlendShape blendShape, bool applyModifiedProperties)
        {
            bool alreadyExists = blendShapes.Transform(
                sp => ConfigSerialization.DeserializeBlendShape(sp) is AnimatableBlendShape current
                    && current.skinnedMeshRenderer == blendShape.skinnedMeshRenderer
                    && current.name == blendShape.name,
                sp => sp.FindPropertyRelative(nameof(AnimatableBlendShape.key)).stringValue = blendShape.key,
                applyModifiedProperties);

            if (!alreadyExists)
            {
                _ = blendShapes.Add(
                sp => ConfigSerialization.SerializeBlendShape(sp, blendShape),
                applyModifiedProperties);
            }
        }

        internal void RemoveBlendShape(AnimatableBlendShape blendShape, bool applyModifiedProperties)
        {
            if (configurator.Reference.IsBlendShapeSelected(
                blendShape.skinnedMeshRenderer,
                blendShape.name,
                out _,
                out AnimatableBlendShape existing)
                && existing.key == blendShape.key)
            {
                _ = blendShapes.Transform(
                    sp => sp.FindPropertyRelative(nameof(AnimatableBlendShape.key)).stringValue == blendShape.key,
                    sp => sp.FindPropertyRelative(nameof(AnimatableBlendShape.disabled)).boolValue = true,
                    applyModifiedProperties);
            }
            else
            {
                blendShapes.Remove(
                    sp => ConfigSerialization.DeserializeBlendShape(sp).Matches(blendShape),
                    applyModifiedProperties);
            }
        }

        internal void AddGameObject(AnimatableGameObject gameObject, bool applyModifiedProperties)
        {
            bool alreadyExists = blendShapes.Transform(
                sp => ConfigSerialization.DeserializeGameObject(sp) is AnimatableGameObject current
                    && current.gameObject == gameObject.gameObject,
                sp => sp.FindPropertyRelative(nameof(AnimatableGameObject.key)).stringValue = gameObject.key,
                applyModifiedProperties);

            if (!alreadyExists)
            {
                _ = gameObjects.Add(
                    sp => ConfigSerialization.SerializeGameObject(sp, gameObject),
                    applyModifiedProperties);
            }
        }

        internal void RemoveGameObject(AnimatableGameObject gameObject, bool applyModifiedProperties)
        {
            if (configurator.Reference.IsGameObjectSelected(
                gameObject.gameObject,
                out AnimatableGameObject existing)
                && existing.key == gameObject.key)
            {
                _ = blendShapes.Transform(
                    sp => sp.FindPropertyRelative(nameof(AnimatableGameObject.key)).stringValue == gameObject.key,
                    sp => sp.FindPropertyRelative(nameof(AnimatableGameObject.disabled)).boolValue = true,
                    applyModifiedProperties);
            }
            else
            {
                gameObjects.Remove(
                            sp => ConfigSerialization.DeserializeGameObject(sp).Matches(gameObject),
                            applyModifiedProperties);
            }
        }
    }
}
