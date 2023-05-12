namespace EZUtils.EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;
    using System.Linq;

    using static Localization;
    using EZUtils.Localization.UIElements;

    [CustomEditor(typeof(AnimatorLayerComponent))]
    public class AnimatorLayerComponentEditor : Editor
    {
        [UnityEngine.SerializeField] private VisualTreeAsset uxml;
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement visualElement = uxml.CloneTree();
            TranslateElementTree(visualElement);
            //is more convenient to do this immediately, in our case
            visualElement.Bind(serializedObject);

            AnimatorLayerComponent target = (AnimatorLayerComponent)this.target;

            visualElement.Q<Toolbar>().AddLocaleSelector();

            ConfigurationOperations configOperations = new ConfigurationOperations(serializedObject, target.gameObject.scene);

            VisualElement referenceContainer = visualElement.Q<VisualElement>(name: "reference-animation-container");
            AnimationConfigurationField referenceField =
                new AnimationConfigurationField(configOperations, isReferenceAnimation: true);
            referenceField.Rebind(serializedObject.FindProperty("referenceAnimation"));
            referenceContainer.Add(referenceField);

            VisualElement animationContainer = visualElement.Q<VisualElement>(name: "other-animation-container");
            SerializedProperty animationsArray = serializedObject.FindProperty("animations");
            SerializedPropertyContainer animations = SerializedPropertyContainer.CreateSimple(
                animationContainer,
                animationsArray,
                () => new AnimationConfigurationField(configOperations, isReferenceAnimation: false));

            DefaultAnimationPopupField defaultAnimationPopup =
                DefaultAnimationPopupField.Create(target.referenceAnimation, target.animations);
            _ = defaultAnimationPopup.RegisterValueChangedCallback(evt =>
            {
                Utilities.RecordChange(target, "Set default animation", layer =>
                {
                    target.referenceAnimation.isDefaultAnimation = target.referenceAnimation == evt.newValue;

                    foreach (AnimationConfiguration animation in target.animations)
                    {
                        animation.isDefaultAnimation = animation == evt.newValue;
                    }
                });
                serializedObject.Update();
                //no other refreshing to do
            });
            visualElement.Q<VisualElement>(name: "defaultAnimationPopup").Add(defaultAnimationPopup);

            //not a big fan of initialization, and there's a bit of a circular reference thing going on here
            //would like a design that doesn't do this, but it's somewhat difficult with uielements without other
            //crazy stuff, like moving all event handling into this class and out of their respective controls
            //or funneling around custom events, perhaps
            //as long as dependencies of this class don't use them in their ctor, we should be pretty fine though
            //
            //this is also still better than previous designs, where this circular dependency was hidden and this class
            //littered with methods
            configOperations.Initialize(referenceField, animations, defaultAnimationPopup);
            animations.Refresh();

            Toggle hideUnchangedItemsToggle = visualElement.Q<Toggle>(name: "hideUnchangedItems");
            _ = hideUnchangedItemsToggle.RegisterValueChangedCallback(
                evt => visualElement.EnableInClassList("hide-unchanged-items", evt.newValue));
            visualElement.EnableInClassList(
                "hide-unchanged-items", hideUnchangedItemsToggle.value);

            visualElement.Q<Button>(name: "addNewAnimation").clicked += () =>
            {
                Utilities.RecordChange(target, "Add new animation", layer =>
                {
                    //seems rather difficult to do this duplication with just  serializedproperties
                    string name = animations.Count == 0
                        ? target.name :
                        $"{target.name}_{animations.Count}";
                    AnimationConfiguration newAnimation = new AnimationConfiguration() { name = name };
                    newAnimation.blendShapes.AddRange(target.referenceAnimation.blendShapes.Select(bs => bs.Clone()));
                    newAnimation.gameObjects.AddRange(target.referenceAnimation.gameObjects.Select(go => go.Clone()));
                    layer.animations.Add(newAnimation);
                    defaultAnimationPopup.AllAnimations.Add(newAnimation);
                });
                serializedObject.Update();
                animations.RefreshExternalChanges();
            };

            //NOTE: if we find a good way to sync from gameobject name, would love to do that
            //it may be hypothetically possible with some trickery in the component itself
            TextField nameField = visualElement.Q<TextField>(name: "name");
            nameField.isDelayed = true;
            _ = nameField.RegisterValueChangedCallback(evt =>
            {
                if (target.gameObject.GetComponents<AnimatorLayerComponent>().Length > 1) return;
                if (target.gameObject.name != evt.previousValue) return;

                target.gameObject.name = evt.newValue;
            });

            return visualElement;
        }
    }
}
