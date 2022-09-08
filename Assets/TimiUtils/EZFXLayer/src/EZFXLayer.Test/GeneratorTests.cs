namespace EZFXLayer.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class GeneratorTests
    {
        [SetUp]
        public void SetUp() => TestSetup.StandardTestSetUp();

        [Test]
        public void Throws_WhenAvatarHasNoDescriptor()
        {
            TestSetup testSetup = new TestSetup();
            UnityEngine.Object.DestroyImmediate(testSetup.Avatar.GetComponent<VRCAvatarDescriptor>());
            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());

            Assert.That(
                () => generator.Generate(testSetup.Avatars, testSetup.Assets),
                Throws.InvalidOperationException);
        }

        [Test]
        public void NoChanges_WhenNoLayerConfigs()
        {
            TestSetup testSetup = new TestSetup();
            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());

            generator.Generate(testSetup.Avatars, testSetup.Assets);

            Assert.That(
                testSetup.Assets.FXController.layers,
                HasCountConstraint.Create(0));
            Assert.That(
                testSetup.Assets.Menu.controls,
                HasCountConstraint.Create(0));
            Assert.That(
                testSetup.Assets.Parameters.parameters,
                HasCountConstraint.Create(0));
        }

        //TODO: tests around duplicate layers. we'll allow them for empty layers, tho, useful as markers.
        //well, duplicate layers where one is a marker isn't too useful yet tho has it's rare scenario.
        //for avatar-specific settings eventually, this may be important depending on how we implement that.

        //consider polishing up the generated layers and animations stuff and having that be the generator config.
        //then, the logic to turn components into them moves out of generator and into components themselves, like the
        //reference configuration-containing button or whatever.
        //  processed animation cant have a clip field anymore, since components wont generate the clips; generator will
        //    generator uses the animation component just for blend shapes and game objects. likely, those two things
        //    themselves are not components, so we dont need to duplicate them. processed animation can be given them,
        //    and then it can generate its own clip.
        //  matches state isn't ideal, but it seems slightly better than exposing a single property known as statename. after these changes, it won't expose any state and just methods, which is cool
        //  a little bit up for debate, but we'll make the classes public and its members internal, to keep it ports-and-adaptersy
        //  probably the most obnoxious thing will be changing the generator. or maybe we dont have to, at least not yet.
        //    it's almost certainly better to do so, but, for now, we'll convert components to processed* like we currently do.
        //  and ofc pick better names, since they're no longer internal implementation details pertaining to processing the main config, but are now the main config itself
    }
}
