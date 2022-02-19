namespace EZFXLayer.Test
{
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class Tests
    {
        [Test]
        public void Generate_Throws_WhenAvatarHasNoDescriptor()
        {
            VrcAssets vrcAssets = new VrcAssets();
            GameObject avatar = Avatar.Create("one");
            Object.DestroyImmediate(avatar.GetComponent<VRCAvatarDescriptor>());


            EZFXLayerGenerator generator = new();

            Assert.That(() => generator.Generate(
                Enumerable.Repeat(avatar, 1),
                vrcAssets.FXController,
                vrcAssets.Menu,
                vrcAssets.Parameters), Throws.InvalidOperationException);
        }
    }
}
