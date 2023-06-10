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
        private SerializedObject gameObjectSerializedObject = null;

        [UnityEngine.SerializeField] private VisualTreeAsset uxml;
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement visualElement = uxml.CommonUIClone();
            TranslateElementTree(visualElement);

            //is more convenient to do this immediately, in our case
            //instead of waiting for it to happen later
            visualElement.Bind(serializedObject);

            AnimatorLayerComponent target = (AnimatorLayerComponent)this.target;

            visualElement.Q<LayerCreationButtons>().SetTarget(target.gameObject);

            visualElement.Q<Toolbar>().AddLocaleSelector();

            VisualElement referenceContainer = visualElement.Q<VisualElement>(name: "reference-container");
            VisualElement animationContainer = visualElement.Q<VisualElement>(name: "other-animation-container");
            VisualElement defaultAnimationPopupContainer = visualElement.Q<VisualElement>(name: "defaultAnimationPopup");

            ConfigurationOperations configOperations = new ConfigurationOperations(
                target,
                serializedObject,
                referenceContainer,
                animationContainer,
                defaultAnimationPopupContainer);

            Toggle hideUnchangedItemsToggle = visualElement.Q<Toggle>(name: "hideUnchangedItems");
            _ = hideUnchangedItemsToggle.RegisterValueChangedCallback(
                evt => visualElement.EnableInClassList("hide-unchanged-items", evt.newValue));
            visualElement.EnableInClassList(
                "hide-unchanged-items", hideUnchangedItemsToggle.value);

            visualElement.Q<Button>(name: "addNewAnimation").clicked += () => configOperations.AddNewAnimation();

            TextField nameField = visualElement.Q<TextField>(name: "name");
            nameField.isDelayed = true;
            _ = nameField.RegisterValueChangedCallback(evt =>
            {
                if (target.gameObject.GetComponents<AnimatorLayerComponent>().Length > 1) return;
                if (target.gameObject.name != evt.previousValue) return;

                target.gameObject.name = evt.newValue;
            });

            gameObjectSerializedObject = new SerializedObject(target.gameObject);
            //we use a hidden field in order to get events on gameobject name changes
            TextField objectNameField = visualElement.Q<TextField>(name: "objectName");
            _ = objectNameField.RegisterValueChangedCallback(evt =>
            {
                if (target.gameObject.GetComponents<AnimatorLayerComponent>().Length > 1) return;
                if (target.name != evt.previousValue) return;

                target.name = evt.newValue;
            });
            //binding to the component hasn't happened yet. if we bind to gameobject too early, it will get overwritten
            //so we bind a frame later
            _ = visualElement.schedule.Execute(() => objectNameField.Bind(gameObjectSerializedObject));

            return visualElement;
        }

        private void OnDestroy() => gameObjectSerializedObject?.Dispose();

        [InitializeOnLoadMethod]
        private static void UnityInitialize()
            => AddComponentMenu<AnimatorLayerComponent>("EZFXLayer/EZFXLayer animator layer configuration", priority: 0);
    }
}
