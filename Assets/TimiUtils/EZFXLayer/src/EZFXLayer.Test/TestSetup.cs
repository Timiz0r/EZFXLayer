namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class TestSetup
    {
        public VrcAssets Assets { get; } = new VrcAssets();
        public ConfigurationBuilder ConfigurationBuilder { get; } = new ConfigurationBuilder();
        public GameObject Avatar { get; }
        public IEnumerable<GameObject> Avatars { get; }

        public TestSetup()
        {
            Avatars = VrcAvatar.Create("foo");
            Avatar = Avatars.Single();
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
