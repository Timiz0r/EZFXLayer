namespace EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class TestSetup
    {
        public VrcAssets Assets { get; } = new VrcAssets();
        public ConfigurationBuilder ConfigurationBuilder { get; } =
            new ConfigurationBuilder(new GameObject("ezfxlayertest"));
        public GameObject Avatar => Avatars.Single();
        public IEnumerable<GameObject> Avatars { get; } = VrcAvatar.Create("foo");

        public EZFXLayerGenerator CreateGenerator()
        {
            EZFXLayerGenerator generator = new EZFXLayerGenerator(ConfigurationBuilder.Generate());
            return generator;
        }

        public static void StandardTestSetUp()
        {
            //otherwise the ReferenceConfiguration components add up, and there can only be one
            GameObject dummy = new GameObject();
            foreach (UnityEngine.Object obj in dummy.scene.GetRootGameObjects())
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
