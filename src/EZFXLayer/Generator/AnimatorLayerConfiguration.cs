namespace EZUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using static Localization;

    //the reason AnimationConfigurationHelper exists and AnimatorLayerConfigurationHelper doesnt is because
    //this class is a public driver port of the app, and the other is an internal implementation detail of the app
    public class AnimatorLayerConfiguration
    {
        //NOTE: it's definitely easier to instead resort Animations so the toggle animation is first
        //if we do another redesign of toggle values, then we can consider it
        //NOTE: we didnt put these values in AnimationConfigurationHelper not for any particular reason
        //and could probably move it there
        private readonly Dictionary<AnimationConfigurationHelper, int> animationToggleValues;

        internal string Name { get; }
        internal IReadOnlyList<AnimationConfigurationHelper> Animations { get; }
        internal string MenuPath { get; }
        internal bool ManageAnimatorControllerStates { get; }
        internal bool ManageExpressionMenuAndParameters { get; }
        internal IProcessedParameter Parameter { get; }
        internal bool IsMarkerLayer { get; }

        //is not private, since we, by design, allow using this without components (given to Create)
        public AnimatorLayerConfiguration(
            string name,
            IReadOnlyList<AnimationConfiguration> animations,
            string menuPath,
            bool manageAnimatorControllerStates,
            bool manageExpressionMenuAndParameters,
            bool saveExpressionParameters,
            GenerationOptions generationOptions)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Animations = animations?
                .Select(a => new AnimationConfigurationHelper(a, generationOptions))?
                .ToArray()
                ?? throw new ArgumentNullException(nameof(animations));
            MenuPath = menuPath;
            ManageAnimatorControllerStates = manageAnimatorControllerStates;
            ManageExpressionMenuAndParameters = manageExpressionMenuAndParameters;

            animationToggleValues = new Dictionary<AnimationConfigurationHelper, int>(animations.Count);
            int toggleValueOffset = 1;
            int defaultAnimationToggleValue = -1;
            for (int i = 0; i < animations.Count; i++)
            {
                AnimationConfiguration animation = animations[i];
                AnimationConfigurationHelper animationConfigurationHelper = Animations[i];
                int currentToggleValue;

                if (animation.isToggleOffAnimation)
                {
                    //the toggle off animation is always zero
                    //so any animation in the list occurring before it is shifted index higher
                    //and any after it isn't shifted so has the same index
                    toggleValueOffset = 0;
                    currentToggleValue = animationToggleValues[animationConfigurationHelper] = 0;
                }
                else
                {
                    currentToggleValue = animationToggleValues[animationConfigurationHelper] = toggleValueOffset + i;
                }

                if (animation.isDefaultAnimation)
                {
                    defaultAnimationToggleValue = currentToggleValue;
                }
            }

            if (animations.Count > 0 && defaultAnimationToggleValue == -1) throw new ArgumentOutOfRangeException(
                nameof(animations), T("There is no default animation."));
            if (animations.Count > 0 && !animations.Any(a => a.isToggleOffAnimation)) throw new ArgumentOutOfRangeException(
                nameof(animations), T("There is no toggle off animation."));

            Parameter = animations.Count > 2
                ? (IProcessedParameter)new IntProcessedParameter(name, defaultAnimationToggleValue, saveExpressionParameters)
                : new BooleanProcessedParameter(name, defaultAnimationToggleValue != 0, saveExpressionParameters);

            IsMarkerLayer = animations.All(a => a.gameObjects.Count == 0 && a.blendShapes.Count == 0);
        }

        public static AnimatorLayerConfiguration FromComponent(AnimatorLayerComponent layer, GenerationOptions generationOptions)
        {
            layer.PerformComponentUpgrades(out _);
            return layer != null
                ? new AnimatorLayerConfiguration(
                    name: layer.name,
                    animations: layer.animations.ToArray(),
                    menuPath: layer.menuPath,
                    manageAnimatorControllerStates: layer.manageAnimatorControllerStates,
                    manageExpressionMenuAndParameters: layer.manageExpressionMenuAndParameters,
                    saveExpressionParameters: layer.saveExpressionParameters,
                    generationOptions: generationOptions ?? throw new ArgumentNullException(nameof(generationOptions)))
                : throw new ArgumentNullException(nameof(layer));
        }

        internal int GetAnimationToggleValue(AnimationConfigurationHelper animation) => animationToggleValues[animation];
    }
}
