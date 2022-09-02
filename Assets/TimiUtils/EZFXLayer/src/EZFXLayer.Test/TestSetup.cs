namespace EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class TestSetup
    {
        public VrcAssets Assets { get; } = new VrcAssets();
        public ConfigurationBuilder ConfigurationBuilder { get; } = new ConfigurationBuilder();
        public GameObject Avatar { get; } = new GameObject("avatar");
        public IEnumerable<GameObject> Avatars { get; }

        public TestSetup()
        {
            Avatars = new[] { Avatar };
        }

        public EZFXLayerGenerator CreateGenerator()
        {
            ConfigurationBuilder.WithReferenceConfiguration(
                Assets.FXController,
                Assets.Menu,
                Assets.Parameters
            );
            EZFXLayerGenerator generator = new EZFXLayerGenerator(ConfigurationBuilder.Generate());
            return generator;
        }
    }
}
