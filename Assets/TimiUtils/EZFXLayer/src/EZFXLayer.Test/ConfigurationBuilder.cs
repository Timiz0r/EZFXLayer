namespace EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    //TODO: this could hypothetically go into the editor assembly, allowing those to author these configs as code isntead of components
    //which decent redesign to interact with a scene, anyway. wonder if tests will try to do that :thinking:
    public class ConfigurationBuilder
    {
        private readonly List<AnimatorLayerConfiguration> layers = new List<AnimatorLayerConfiguration>();
        private ReferenceConfiguration referenceConfiguration;
        //not allowed to instantiate a MonoBehavior like ReferenceConfiguration
        private readonly GameObject rootGameObject = new GameObject("configurationbuilder");

        public void WithReferenceConfiguration(
            AnimatorController fxController,
            VRCExpressionsMenu menu,
            VRCExpressionParameters parameters)
        {
            //this needs some rethinking into how we want to deal with gameobjects here
            //but for now we get desired behavior and a good test
            if (referenceConfiguration != null) throw new ArgumentOutOfRangeException(
                nameof(referenceConfiguration), "A reference configuration was already added.");

            referenceConfiguration = rootGameObject.AddComponent<ReferenceConfiguration>();
            referenceConfiguration.fxLayerController = fxController;
            referenceConfiguration.vrcRootExpressionsMenu = menu;
            referenceConfiguration.vrcExpressionParameters = parameters;
        }

        public EZFXLayerConfiguration Generate() => new EZFXLayerConfiguration(referenceConfiguration, layers);
    }
}
