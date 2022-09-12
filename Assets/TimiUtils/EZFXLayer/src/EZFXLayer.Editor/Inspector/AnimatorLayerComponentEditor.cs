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
                animations.Refresh();
            };

            return visualElement;
        }

        public void DeleteBlendShape(AnimatableBlendShape blendShape)
        {
            //TODO: these will probably create a lot of undos
            //so need a mechanism to unify applies, probably just by not doing it in the serializedpropertycontainer
            //(or an option)
            referenceField.DeleteBlendShape(blendShape);

        }

        public void AddBlendShape(AnimatableBlendShape blendShape)
        {
            referenceField.AddBlendShape(blendShape);
        }
    }
}
