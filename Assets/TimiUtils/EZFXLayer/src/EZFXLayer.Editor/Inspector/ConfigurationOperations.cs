namespace EZFXLayer.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    internal class ConfigurationOperations
    {
        private AnimationConfigurationField referenceField;
        private SerializedPropertyContainer animations;
        private readonly SerializedObject serializedObject;
        private readonly Scene scene;

        public ConfigurationOperations(SerializedObject serializedObject, Scene scene)
        {
            this.serializedObject = serializedObject;
            this.scene = scene;
        }

        public void Initialize(
            AnimationConfigurationField referenceField,
            SerializedPropertyContainer animations)
        {
            this.referenceField = referenceField;
            this.animations = animations;
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

        //while other things use their deserialized objects, we just use key here because it's all we need
        //and deserialization of serializedproperty is obnoxious
        public void RemoveAnimation(string animationConfigurationKey) => animations.Remove(
            sp => sp.FindPropertyRelative(nameof(AnimationConfiguration.key)).stringValue == animationConfigurationKey);

        public void SelectBlendShapes(Rect buttonBox)
        {
            BlendShapeSelectorPopup.Show(buttonBox, this, scene);
        }

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

        public bool HasBlendShape(SkinnedMeshRenderer skinnedMeshRenderer, string name)
            => referenceField.BlendShapes
                .Select(bs => bs.BlendShape)
                .Any(bs => bs.skinnedMeshRenderer == skinnedMeshRenderer && bs.name == name);

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
    }
}
