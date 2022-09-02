namespace EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.Animations;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class ConfigurationBuilder
    {
        private readonly List<AnimatorLayerConfiguration> layers = new List<AnimatorLayerConfiguration>();
        private ReferenceConfiguration referenceConfiguration;

        public void WithReferenceConfiguration(
            AnimatorController fxController,
            VRCExpressionsMenu menu,
            VRCExpressionParameters parameters)
            => referenceConfiguration = new ReferenceConfiguration()
            {
                fxLayerController = fxController,
                vrcRootExpressionsMenu = menu,
                vrcExpressionParameters = parameters
            };

        public EZFXLayerConfiguration Generate() => new EZFXLayerConfiguration(referenceConfiguration, layers);
    }
}
