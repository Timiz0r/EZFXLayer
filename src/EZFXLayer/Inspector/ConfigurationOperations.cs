namespace EZUtils.EZFXLayer.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;

    internal class ConfigurationOperations
    {
        private readonly SerializedObject serializedObject;
        private readonly AnimatorLayerComponent component;
        private readonly Scene scene;

        private readonly ReferenceAnimatablesField referenceField;
        private readonly SerializedPropertyContainer animations;
        private readonly DefaultAnimationPopupField defaultAnimationPopup;

        public ConfigurationOperations(
            AnimatorLayerComponent component,
            SerializedObject serializedObject,
            VisualElement referenceContainer,
            VisualElement animationContainer,
            VisualElement defaultAnimationPopupContainer)
        {
            this.serializedObject = serializedObject;
            this.component = component;
            scene = component.gameObject.scene;

            referenceField = new ReferenceAnimatablesField(this);
            referenceField.Rebind(serializedObject.FindProperty("referenceAnimatables"));
            referenceContainer.Add(referenceField);

            SerializedProperty animationsArray = serializedObject.FindProperty("animations");
            animations = SerializedPropertyContainer.CreateSimple(
                animationContainer,
                animationsArray,
                () => new AnimationConfigurationField(this));
            animations.Refresh();

            defaultAnimationPopup = new DefaultAnimationPopupField(component.animations);
            _ = defaultAnimationPopup.RegisterValueChangedCallback(evt =>
            {
                Utilities.RecordChange(component, "Set default animation", layer =>
                {
                    foreach (AnimationConfiguration animation in component.animations)
                    {
                        animation.isDefaultAnimation = animation == evt.newValue;
                    }
                });
                serializedObject.Update();
                //no other refreshing to do
            });
            defaultAnimationPopupContainer.Add(defaultAnimationPopup);
        }

        public void AddNewAnimation()
        {
            Utilities.RecordChange(component, "Add new animation", layer =>
            {
                //seems rather difficult to do this duplication with just  serializedproperties
                string name = animations.Count == 0
                    ? component.name :
                    $"{component.name}_{animations.Count}";
                AnimationConfiguration newAnimation = new AnimationConfiguration() { name = name };
                newAnimation.blendShapes.AddRange(component.referenceAnimatables.blendShapes.Select(bs => bs.Clone()));
                newAnimation.gameObjects.AddRange(component.referenceAnimatables.gameObjects.Select(go => go.Clone()));
                layer.animations.Add(newAnimation);
                defaultAnimationPopup.Animations.Add(newAnimation);
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

            AnimationConfiguration animationToRemove =
                defaultAnimationPopup.Animations.Single(a => a.key == animationConfigurationKey);
            _ = defaultAnimationPopup.Animations.Remove(animationToRemove);
            if (animationToRemove.isDefaultAnimation)
            {
                defaultAnimationPopup.SetValueWithoutNotify(defaultAnimationPopup.Animations[0]);
            }
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

        public bool HasBlendShape(SkinnedMeshRenderer skinnedMeshRenderer, string name)
            => referenceField.BlendShapes.AllElements<AnimatableBlendShapeField>()
                .Select(bs => bs.BlendShape)
                .Any(bs => bs.skinnedMeshRenderer == skinnedMeshRenderer && bs.name == name);

        public void SelectBlendShapes(Rect buttonBox, bool isReference)
            => BlendShapeSelectorPopup.Show(buttonBox, this, scene, isReference);

        //was attempting, and could have succeeded, to manually handle undo change recordings and refreshing
        //but for the reference field, we'd need to rebind to trigger an update, since serializedObject.Update
        //doesn't seem to trigger it. the design improved a decent amount, but this one workaround was looking hacky
        //and prob has a perf impact, though minor in our use case.
        public void RemoveReferenceBlendShape(AnimatableBlendShape blendShape)
        {
            referenceField.BlendShapes.Remove(
                sp => ConfigSerialization.DeserializeBlendShape(sp).Matches(blendShape), apply: false);

            foreach (AnimationConfigurationField element in animations.AllElements<AnimationConfigurationField>())
            {
                element.BlendShapes.Remove(
                    sp => ConfigSerialization.DeserializeBlendShape(sp).Matches(blendShape),
                    apply: false);
            }

            //for undo reasons, we've suppressed this call until this point
            _ = serializedObject.ApplyModifiedProperties();
        }

        public void RemoveReferenceBlendShape(SkinnedMeshRenderer skinnedMeshRenderer, string name)
            => RemoveReferenceBlendShape(
                referenceField.BlendShapes.AllElements<AnimatableBlendShapeField>()
                    .Select(bs => bs.BlendShape)
                    .Single(bs => bs.skinnedMeshRenderer == skinnedMeshRenderer && bs.name == name));

        public void AddReferenceBlendShape(SkinnedMeshRenderer skinnedMeshRenderer, string name)
        {
            AnimatableBlendShape blendShape = new AnimatableBlendShape()
            {
                skinnedMeshRenderer = skinnedMeshRenderer,
                name = name
            };

            _ = referenceField.BlendShapes.Add(
                sp => ConfigSerialization.SerializeBlendShape(sp, blendShape), apply: false);

            foreach (AnimationConfigurationField element in animations.AllElements<AnimationConfigurationField>())
            {
                _ = element.BlendShapes.Add(sp => ConfigSerialization.SerializeBlendShape(sp, blendShape), apply: false);
            }

            _ = serializedObject.ApplyModifiedProperties();
        }

        public void ReferenceBlendShapeChanged()
        {
            IEnumerable<AnimatableBlendShapeField> blendShapes =
                animations.AllElements<AnimationConfigurationField>().SelectMany(
                    a => a.BlendShapes.AllElements<AnimatableBlendShapeField>());
            foreach (AnimatableBlendShapeField element in blendShapes)
            {
                element.CheckForReferenceMatch();
            }
        }

        public bool BlendShapeMatchesReference(AnimatableBlendShape blendShape)
            => referenceField.BlendShapes.AllElements<AnimatableBlendShapeField>().Any(
                rbs => rbs.BlendShape.Matches(blendShape) && rbs.BlendShape.value == blendShape.value);

        public void RemoveReferenceGameObject(AnimatableGameObject gameObject)
        {
            referenceField.GameObjects.Remove(
                sp => ConfigSerialization.DeserializeGameObject(sp).Matches(gameObject),
                apply: false);

            foreach (AnimationConfigurationField element in animations.AllElements<AnimationConfigurationField>())
            {
                element.GameObjects.Remove(
                    sp => ConfigSerialization.DeserializeGameObject(sp).Matches(gameObject),
                    apply: false);
            }

            //for undo reasons, we've suppressed this call until this point
            _ = serializedObject.ApplyModifiedProperties();
        }

        public void AddReferenceGameObject(AnimatableGameObject gameObject)
        {
            _ = referenceField.GameObjects.Add(
                sp => ConfigSerialization.SerializeGameObject(sp, gameObject),
                apply: false);

            foreach (AnimationConfigurationField element in animations.AllElements<AnimationConfigurationField>())
            {
                _ = element.GameObjects.Add(sp => ConfigSerialization.SerializeGameObject(sp, gameObject), apply: false);
            }

            //for undo reasons, we've suppressed this call until this point
            _ = serializedObject.ApplyModifiedProperties();
        }

        public void ReferenceGameObjectChanged()
        {
            IEnumerable<AnimatableGameObjectField> gameObjects = animations
                .AllElements<AnimationConfigurationField>()
                .SelectMany(a => a.GameObjects.AllElements<AnimatableGameObjectField>());
            foreach (AnimatableGameObjectField element in gameObjects)
            {
                element.CheckForReferenceMatch();
            }
        }

        public bool GameObjectMatchesReference(AnimatableGameObject gameObject) => referenceField.GameObjects
            .AllElements<AnimatableGameObjectField>()
            .Any(go =>
                go.GameObject.Matches(gameObject)
                && go.GameObject.active == gameObject.active);
    }
}
