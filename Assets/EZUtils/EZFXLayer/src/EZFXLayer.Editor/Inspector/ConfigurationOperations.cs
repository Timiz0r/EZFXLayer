namespace EZUtils.EZFXLayer.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    internal class ConfigurationOperations
    {
        private AnimationConfigurationField referenceField;
        private SerializedPropertyContainer animations;
        private DefaultAnimationPopupField defaultAnimationPopup;
        private readonly SerializedObject serializedObject;
        private readonly Scene scene;

        public ConfigurationOperations(SerializedObject serializedObject, Scene scene)
        {
            this.serializedObject = serializedObject;
            this.scene = scene;
        }

        public void Initialize(
            AnimationConfigurationField referenceField,
            SerializedPropertyContainer animations,
            DefaultAnimationPopupField defaultAnimationPopup)
        {
            this.referenceField = referenceField;
            this.animations = animations;
            this.defaultAnimationPopup = defaultAnimationPopup;
        }

        //while other things use their deserialized objects, we just use key here because it's all we need
        //and deserialization of serializedproperty is obnoxious
        public void RemoveAnimation(string animationConfigurationKey)
        {
            animations.Remove(
                sp => sp.FindPropertyRelative(nameof(AnimationConfiguration.key)).stringValue == animationConfigurationKey);

            //ideally, we would also propagate a change to make the reference animation default if the deleted animation
            //was default, but that's unfortunately hard to do without putting it into a separate undo.
            //luckily, we can treat no configured default animation as the reference being default
            //so we're pretty okay with this
            AnimationConfiguration animationToRemove =
                defaultAnimationPopup.AllAnimations.Single(a => a.key == animationConfigurationKey);
            _ = defaultAnimationPopup.AllAnimations.Remove(animationToRemove);
            if (animationToRemove.isDefaultAnimation)
            {
                defaultAnimationPopup.SetValueWithoutNotify(defaultAnimationPopup.AllAnimations[0]);
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
            => referenceField.BlendShapes
                .Select(bs => bs.BlendShape)
                .Any(bs => bs.skinnedMeshRenderer == skinnedMeshRenderer && bs.name == name);

        public void SelectBlendShapes(Rect buttonBox) => BlendShapeSelectorPopup.Show(buttonBox, this, scene);

        //was attempting, and could have succeeded, to manually handle undo change recordings and refreshing
        //but for the reference field, we'd need to rebind to trigger an update, since serializedObject.Update
        //doesn't seem to trigger it. the design improved a decent amount, but this one workaround was looking hacky
        //and prob has a perf impact, though minor in our use case.
        //TODO: instead of all the bubbling thru like this, just expose the damn property
        public void RemoveBlendShape(AnimatableBlendShape blendShape)
        {
            referenceField.RemoveBlendShape(blendShape);

            foreach (AnimationConfigurationField element in animations.AllElements<AnimationConfigurationField>())
            {
                element.RemoveBlendShape(blendShape);
            }

            //for undo reasons, we've suppressed this call until this point
            _ = serializedObject.ApplyModifiedProperties();
        }

        public void RemoveBlendShape(SkinnedMeshRenderer skinnedMeshRenderer, string name) => RemoveBlendShape(
            referenceField.BlendShapes
                .Select(bs => bs.BlendShape)
                .Single(bs => bs.skinnedMeshRenderer == skinnedMeshRenderer && bs.name == name));

        public void AddBlendShape(SkinnedMeshRenderer skinnedMeshRenderer, string name)
        {
            AnimatableBlendShape blendShape = new AnimatableBlendShape()
            {
                skinnedMeshRenderer = skinnedMeshRenderer,
                name = name
            };

            referenceField.AddBlendShape(blendShape);

            foreach (AnimationConfigurationField element in animations.AllElements<AnimationConfigurationField>())
            {
                element.AddBlendShape(blendShape);
            }

            _ = serializedObject.ApplyModifiedProperties();
        }

        public void ReferenceBlendShapeChanged()
        {
            IEnumerable<AnimatableBlendShapeField> blendShapes =
                animations.AllElements<AnimationConfigurationField>().SelectMany(a => a.BlendShapes);
            foreach (AnimatableBlendShapeField element in blendShapes)
            {
                element.CheckForReferenceMatch();
            }
        }

        public bool BlendShapeMatchesReference(AnimatableBlendShape blendShape)
            => referenceField.BlendShapes.Any(
                rbs => rbs.BlendShape.Matches(blendShape) && rbs.BlendShape.value == blendShape.value);

        public void RemoveGameObject(AnimatableGameObject gameObject)
        {
            referenceField.RemoveGameObject(gameObject);

            foreach (AnimationConfigurationField element in animations.AllElements<AnimationConfigurationField>())
            {
                element.RemoveGameObject(gameObject);
            }

            //for undo reasons, we've suppressed this call until this point
            _ = serializedObject.ApplyModifiedProperties();
        }

        public void AddGameObject(AnimatableGameObject gameObject)
        {
            referenceField.AddGameObject(gameObject);

            foreach (AnimationConfigurationField element in animations.AllElements<AnimationConfigurationField>())
            {
                element.AddGameObject(gameObject);
            }

            _ = serializedObject.ApplyModifiedProperties();
        }

        public void ReferenceGameObjectChanged()
        {
            IEnumerable<AnimatableGameObjectField> gameObjects =
                animations.AllElements<AnimationConfigurationField>().SelectMany(a => a.GameObjects);
            foreach (AnimatableGameObjectField element in gameObjects)
            {
                element.CheckForReferenceMatch();
            }
        }

        public bool GameObjectMatchesReference(AnimatableGameObject gameObject)
            => referenceField.GameObjects.Any(
                go => go.GameObject.Matches(gameObject) && go.GameObject.active == gameObject.active);
    }
}
