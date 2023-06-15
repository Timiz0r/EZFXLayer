namespace EZUtils.EZFXLayer.Test
{
    using System;
    using System.Linq;
    using EZFXLayer;
    using UnityEngine;

    public class AnimationConfigurationBuilder
    {
        private readonly AnimatorLayerComponent layer;
        private readonly AnimationConfiguration animation;

        public AnimationConfigurationBuilder(string name, AnimatorLayerComponent layer)
        {
            if (layer == null) throw new ArgumentNullException(nameof(layer));

            layer.animations.Add(animation = AnimationConfiguration.Create(name));
            this.layer = layer;
        }

        public AnimationConfigurationBuilder SetGameObject(GameObject gameObject, bool isActive, bool disabled = false)
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            if (animation.gameObjects.SingleOrDefault(go => go.gameObject == gameObject) is AnimatableGameObject existing)
            {
                existing.active = isActive;
                existing.disabled = disabled;
            }
            else
            {
                animation.gameObjects.Add(new AnimatableGameObject(
                    gameObject,
                    path: null,
                    active: isActive,
                    synchronizeActiveWithReference: false,
                    disabled: disabled));
            }

            return this;
        }

        public AnimationConfigurationBuilder SetBlendShape(
            SkinnedMeshRenderer skinnedMeshRenderer, string blendShapeName, float value, bool disabled = false)
        {
            if (skinnedMeshRenderer == null) throw new ArgumentNullException(nameof(skinnedMeshRenderer));
            if (string.IsNullOrEmpty(blendShapeName)) throw new ArgumentNullException(nameof(blendShapeName));

            if (animation.blendShapes.SingleOrDefault(
                bs => bs.skinnedMeshRenderer == skinnedMeshRenderer
                    && bs.name == blendShapeName) is AnimatableBlendShape existing)
            {
                existing.value = value;
                existing.disabled = disabled;
            }
            else
            {
                animation.blendShapes.Add(new AnimatableBlendShape(
                skinnedMeshRenderer,
                blendShapeName,
                value,
                synchronizeValueWithReference: false,
                disabled));
            }

            return this;
        }

        public AnimationConfigurationBuilder MakeDefaultAnimation()
        {
            foreach (AnimationConfiguration animation in layer.animations)
            {
                animation.isDefaultAnimation = false;
            }
            animation.isDefaultAnimation = true;

            return this;
        }

        public AnimationConfigurationBuilder MakeToggleOffAnimation()
        {
            foreach (AnimationConfiguration animation in layer.animations)
            {
                animation.isToggleOffAnimation = false;
            }
            animation.isToggleOffAnimation = true;

            return this;
        }

        public AnimationConfigurationBuilder WithToggleName(string toggleName)
        {
            animation.customToggleName = toggleName;

            return this;
        }

        public AnimationConfigurationBuilder WithStateName(string stateName)
        {
            animation.customAnimatorStateName = stateName;

            return this;
        }

        public AnimationConfigurationBuilder Mutate(Action<AnimationConfiguration> mutator)
        {
            mutator?.Invoke(animation);
            return this;
        }
    }
}
