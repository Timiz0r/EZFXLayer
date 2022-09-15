namespace EZFXLayer.UIElements
{
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;
    using VRC.SDK3.Avatars.ScriptableObjects;
    using System.Linq;
    using System;
    using System.Collections.Generic;

    [CustomEditor(typeof(AnimatorLayerComponent))]
    public class AnimatorLayerComponentEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/AnimatorLayerComponentEditor.uxml");
            VisualElement visualElement = visualTree.CloneTree();
            //is more convenient to do this immediately, in our case
            visualElement.Bind(serializedObject);

            AnimatorLayerComponent target = (AnimatorLayerComponent)this.target;

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

            //not a big fan of initialization, and there's a bit of a circular reference thing going on here
            //would like a design that doesn't do this, but it's somewhat difficult with uielements without other
            //crazy stuff, like moving all event handling into this class and out of their respective controls
            //or funneling around custom events, perhaps
            //as long as dependencies of this class don't use them in their ctor, we should be pretty fine though
            //
            //this is also still better than previous designs, where this circular dependency was hidden and this class
            //littered with methods
            configOperations.Initialize(referenceField, animations);
            animations.Refresh();



            visualElement.Q<UnityEngine.UIElements.Button>(name: "addNewAnimation").clicked += () =>
            {
                Utilities.RecordChange(target, "Add new animation", layer =>
                {
                    //seems rather difficult to do this duplication with just  serializedproperties
                    AnimationConfiguration newAnimation = new AnimationConfiguration() { name = $"{target.name}_{animations.Count}" };
                    newAnimation.blendShapes.AddRange(target.referenceAnimation.blendShapes.Select(bs => bs.Clone()));
                    newAnimation.gameObjects.AddRange(target.referenceAnimation.gameObjects.Select(go => go.Clone()));
                    layer.animations.Add(newAnimation);
                });
                serializedObject.Update();
                animations.RefreshExternalChanges();
            };

            Toggle hideUnchangedItemsToggle = visualElement.Q<Toggle>(name: "hideUnchangedItems");
            _ = hideUnchangedItemsToggle.RegisterValueChangedCallback(
                evt => visualElement.EnableInClassList("hide-unchanged-items", evt.newValue));
            visualElement.EnableInClassList(
                "hide-unchanged-items", hideUnchangedItemsToggle.value);

            return visualElement;
        }
    }
}
