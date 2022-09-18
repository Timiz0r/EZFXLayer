namespace EZUtils.EZFXLayer.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class TestSetup
    {
        public VrcAssets Assets { get; } = new VrcAssets();
        public ConfigurationBuilder ConfigurationBuilder { get; }
        public GameObject Avatar => Avatars.Single();
        public IEnumerable<GameObject> Avatars { get; } = VrcAvatar.Create("foo");

        public TestSetup()
        {
            ConfigurationBuilder = new ConfigurationBuilder(new GameObject("ezfxlayertest"), Assets);
        }

        public void StandardGenerate()
        {
            EZFXLayerGenerator generator = new EZFXLayerGenerator(ConfigurationBuilder.Generate());
            generator.Generate(Assets);
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
