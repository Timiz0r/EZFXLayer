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
                () => _ = generator.Generate(testSetup.Avatars, testSetup.Assets),
                Throws.InvalidOperationException);
        }

        [Test]
        public void NoChanges_WhenNoLayerConfigs()
        {
            TestSetup testSetup = new TestSetup();
            EZFXLayerGenerator generator = new EZFXLayerGenerator(testSetup.ConfigurationBuilder.Generate());

            _ = generator.Generate(testSetup.Avatars, testSetup.Assets);

            Assert.That(
                testSetup.Assets.FXController.layers,
                HasCountConstraint.Create(testSetup.Assets.OriginalFXController.layers));
            Assert.That(
                testSetup.Assets.Menu.controls,
                HasCountConstraint.Create(testSetup.Assets.OriginalMenu.controls));
            Assert.That(
                testSetup.Assets.OriginalParameters.parameters,
                HasCountConstraint.Create(testSetup.Assets.OriginalParameters.parameters));
        }

        //instead of accumulating fat lists for the result of the generation, which is for the purpose of asset management
        //consider having a driven port/interface deal with it
        //"leaking" that there are a specific few kinds of assets to save is likely inevitable, unless we can actually think of a
        //generic way to do it. it's arguably a bit better than putting more asset saving implementation details in the
        //generator and just abstracting away the specific parts of saving them.
        //
        //animations and submenus are easy, since they sit in their own files.
        //for assets within assets, options:
        //  let the generator know that state machine stuff should be saved in the controller, which we already do
        //  just have SaveTransition, SaveState, and SaveStateMachine just pass their respective artifacts and not the controller
        //    since the adapter can just be given the controller thru other means like DI.
        //

        //consider polishing up the generated layers and animations stuff and having that be the generator config.
        //then, the logic to turn components into them moves out of generator and into components themselves, like the
        //reference configuration-containing button or whatever.
        //  previousLayerName prob becomes a parameter
        //  requires a bit more thought, but animation index can probably come out in some way, since the layer has a list and therefore index
        //  IsToBeDefaultState is a weird name, simplify it
        //  IsToBeDefaultState is consumed by the layer to decide if to generate a toggle. instead, just have the anim return a null toggle
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
