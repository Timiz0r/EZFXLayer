namespace EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    public class TestSetup
    {
        public VrcAssets Assets { get; } = new VrcAssets();
        public ConfigurationBuilder ConfigurationBuilder { get; } = new ConfigurationBuilder();
        public GameObject Avatar { get; }
        public IEnumerable<GameObject> Avatars { get; }

        public TestSetup()
        {
            Avatar = new GameObject("avatar");
            _ = Avatar.AddComponent<VRCAvatarDescriptor>();
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
