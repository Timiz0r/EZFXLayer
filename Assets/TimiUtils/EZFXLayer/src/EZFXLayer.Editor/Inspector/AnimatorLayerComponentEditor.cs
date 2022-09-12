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

    [CustomEditor(typeof(AnimatorLayerComponent))]
    public class AnimatorLayerComponentEditor : Editor
    {
        private AnimationConfigurationField referenceField;
        private SerializedPropertyContainer<AnimationConfigurationField> animations;

        public override VisualElement CreateInspectorGUI()
        {
            AnimatorLayerComponent target = (AnimatorLayerComponent)this.target;

            ViewModelVisualElement visualElement = new ViewModelVisualElement()
            {
                ViewModel = this
            };
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/AnimatorLayerComponentEditor.uxml");
            visualTree.CloneTree(visualElement);

            BindableElement referenceContainer = visualElement.Q<BindableElement>(name: "reference-animation-container");
            referenceField = new AnimationConfigurationField(editor: this);
            referenceField.Rebind(serializedObject.FindProperty("referenceAnimation"));
            referenceContainer.Add(referenceField);

            BindableElement animationContainer = visualElement.Q<BindableElement>(name: "other-animation-container");
            SerializedProperty animationsArray = serializedObject.FindProperty("animations");
            animations = new SerializedPropertyContainer<AnimationConfigurationField>(
                animationContainer, animationsArray, () => new AnimationConfigurationField(editor: this));
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

            return visualElement;
        }

        //while other things use their deserialized objects, we just use key here because it's all we need
        //and deserialization of serializedproperty is obnoxious
        public void RemoveAnimation(string animationConfigurationKey)
        {
            animations.Remove(
                sp => sp.FindPropertyRelative(nameof(AnimationConfiguration.key)).stringValue == animationConfigurationKey);
        }

        //was attempting, and could have succeeded, to manually handle undo change recordings and refreshing
        //but for the reference field, we'd need to rebind to trigger an update, since serializedObject.Update
        //doesn't seem to trigger it. the design improved a decent amount, but this one workaround was looking hacky
        //and prob has a perf impact, though minor in our use case.
        public void RemoveBlendShape(AnimatableBlendShape blendShape)
        {
            referenceField.RemoveBlendShape(blendShape);

            animations.ForEachElement(e => e.RemoveBlendShape(blendShape));

            //for undo reasons, we've suppressed this call until this point
            _ = serializedObject.ApplyModifiedProperties();
        }

        public void AddBlendShape(AnimatableBlendShape blendShape)
        {
            referenceField.AddBlendShape(blendShape);

            animations.ForEachElement(e => e.AddBlendShape(blendShape));

            _ = serializedObject.ApplyModifiedProperties();
        }
    }
}
