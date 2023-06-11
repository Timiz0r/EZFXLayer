namespace EZUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EZUtils.EZFXLayer.UIElements;
    using UnityEditor;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;

    public class AnimatorLayerConfigurator
    {
        private readonly AnimatorLayerComponent layerComponent;
        private readonly SerializedObject serializedObject;
        private readonly SerializedPropertyContainer animations;
        private readonly DefaultAnimationPopupField defaultAnimationPopup;

        public ReferenceAnimatablesField Reference { get; }
        public IEnumerable<AnimationConfigurationField> Animations => animations.AllElements<AnimationConfigurationField>();
        public Scene Scene => layerComponent.gameObject.scene;

        public AnimatorLayerConfigurator(
            AnimatorLayerComponent layerComponent,
            SerializedObject serializedObject,
            VisualElement referenceContainer,
            VisualElement animationContainer,
            VisualElement defaultAnimationPopupContainer)
        {
            this.layerComponent = layerComponent;
            this.serializedObject = serializedObject;

            Reference = new ReferenceAnimatablesField(this);
            referenceContainer.Add(Reference);
            Reference.Rebind(serializedObject.FindProperty("referenceAnimatables"));

            SerializedProperty animationsArray = serializedObject.FindProperty("animations");
            animations = SerializedPropertyContainer.CreateSimple(
                animationContainer,
                animationsArray,
                () => new AnimationConfigurationField(this));
            animations.Refresh();

            defaultAnimationPopup = DefaultAnimationPopupField.Create(layerComponent.animations);
            defaultAnimationPopupContainer.Add(defaultAnimationPopup);
            _ = defaultAnimationPopup.RegisterValueChangedCallback(evt =>
            {
                Utilities.RecordChange(this.layerComponent, "Set default animation", layer =>
                {
                    foreach (AnimationConfiguration animation in this.layerComponent.animations)
                    {
                        animation.isDefaultAnimation = animation == evt.newValue;
                    }
                });
                this.serializedObject.Update();
                //no other refreshing to do
            });
        }

        //layer addition and removal need to go here because we get a circular dependency if we put the layer component editor here
        //TODO: need a better class name
        public void AddNewAnimation()
        {
            Utilities.RecordChange(layerComponent, "Add new animation", layer =>
            {
                //seems rather difficult to do this duplication with just  serializedproperties
                string name = animations.Count == 0
                    ? layerComponent.name :
                    $"{layerComponent.name}_{animations.Count}";
                AnimationConfiguration newAnimation = new AnimationConfiguration() { name = name };
                newAnimation.blendShapes.AddRange(layerComponent.referenceAnimatables.blendShapes.Select(bs => bs.Clone()));
                newAnimation.gameObjects.AddRange(layerComponent.referenceAnimatables.gameObjects.Select(go => go.Clone()));
                layer.animations.Add(newAnimation);
                defaultAnimationPopup.Add(newAnimation);
            });
            serializedObject.Update();
            animations.RefreshExternalChanges();
        }

        //while other things use their deserialized objects, we just use key here because it's all we need
        //and deserialization of serializedproperty is obnoxious
        public void RemoveAnimation(string animationConfigurationKey)
        {
            animations.Remove(
                sp => sp.FindPropertyRelative(nameof(AnimationConfiguration.key)).stringValue == animationConfigurationKey);

            defaultAnimationPopup.Remove(animationConfigurationKey);
        }

        public void PropagateDefaultAnimationNameChangeToDefaultAnimationField() =>
            //we're doing this weird trick because this method gets called a bit too early
            //defaultAnimationPopup.value doesn't yet have its name set. if we do it immediately,
            //we're missing the newest chars
            //
            //we used to pass in the new name and do `defaultAnimationPopup.value.name = newName;`,
            //but this caused us to be unable to change the name of the default animation
            //or, rather, the field wasn't set as dirty 🤷‍
            _ = defaultAnimationPopup.schedule.Execute(
                () => defaultAnimationPopup.SetValueWithoutNotify(defaultAnimationPopup.value));
        public void ApplyModifiedProperties() => serializedObject.ApplyModifiedProperties();
        public void Update() => serializedObject.Update();
    }
}
