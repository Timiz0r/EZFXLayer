namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using NUnit.Framework.Constraints;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using VRC.SDK3.Avatars.Components;

    public class ConfigurationBuilderTests
    {
        [SetUp]
        public void SetUp() => TestSetup.StandardTestSetUp();

        //the design doesnt protect from a bunch of user errors around mismatched default animations and other animations
        //this actually helps a lot for testing behavior of the generator
        //and while not as helpful for if we expose this outside of testing, at least the user gets an error at generation
        //and builders are rather readable when used properly, so such errors should be minimal
    }
}
