namespace EZFXLayer.Test
{
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    //we'll also throw controller parameter stuff in here just because
    public class GeneratorExpressionsManagementTests
    {

        [Test]
        public void ChoosesAppropriateParameterType_BasedOnAnimationCount()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "1",
                l => l
                    .ConfigureReferenceAnimation(a => { })
                    .AddAnimation("1", a => { }));
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "2",
                l => l
                    .ConfigureReferenceAnimation(a => { })
                    .AddAnimation("1", a => { })
                    .AddAnimation("2", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(
                testSetup.Assets.FXController.parameters[0].type, Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(
                testSetup.Assets.FXController.parameters[1].type, Is.EqualTo(AnimatorControllerParameterType.Int));

            Assert.That(
                testSetup.Assets.Parameters.parameters[0].valueType, Is.EqualTo(VRCExpressionParameters.ValueType.Bool));
            Assert.That(
                testSetup.Assets.Parameters.parameters[1].valueType, Is.EqualTo(VRCExpressionParameters.ValueType.Int));
        }

        [Test]
        public void DoesNotChangeSavedSetting_IfParameterIsPreExisting()
        {
            TestSetup testSetup = new TestSetup();
            testSetup.Assets.Parameters.parameters = new[]
            {
                new VRCExpressionParameters.Parameter()
                {
                    name = "1",
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    saved = false
                },
                new VRCExpressionParameters.Parameter()
                {
                    name = "2",
                    valueType = VRCExpressionParameters.ValueType.Bool,
                    saved = true
                }
            };
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .AddAnimation("foo", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Parameters.parameters[0].saved, Is.False);
            Assert.That(testSetup.Assets.Parameters.parameters[1].saved, Is.True);
        }

        [Test]
        public void ChangesParameterType_IfNotRightTypeVersusAnimationCount()
        {
            TestSetup testSetup = new TestSetup();

            testSetup.Assets.FXController.AddParameter("1", AnimatorControllerParameterType.Int);
            testSetup.Assets.FXController.AddParameter("2", AnimatorControllerParameterType.Bool);
            testSetup.Assets.Parameters.parameters = new[]
            {
                new VRCExpressionParameters.Parameter()
                {
                    name = "1",
                    valueType = VRCExpressionParameters.ValueType.Int
                },
                new VRCExpressionParameters.Parameter()
                {
                    name = "2",
                    valueType = VRCExpressionParameters.ValueType.Bool
                }
            };

            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(
                testSetup.Assets.FXController.parameters[0].type, Is.EqualTo(AnimatorControllerParameterType.Bool));
            Assert.That(
                testSetup.Assets.FXController.parameters[1].type, Is.EqualTo(AnimatorControllerParameterType.Int));

            Assert.That(
                testSetup.Assets.Parameters.parameters[0].valueType, Is.EqualTo(VRCExpressionParameters.ValueType.Bool));
            Assert.That(
                testSetup.Assets.Parameters.parameters[1].valueType, Is.EqualTo(VRCExpressionParameters.ValueType.Int));
        }

        [Test]
        public void ParameterDefaultValue_IsBasedOnIsDefaultAnimation()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .ConfigureReferenceAnimation(a => { })
                        .AddAnimation("foo", a => a.MakeDefaultAnimation()))
                .AddLayer(
                    "2",
                    l => l
                        .ConfigureReferenceAnimation(a => a.MakeDefaultAnimation())
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "3",
                    l => l
                        .ConfigureReferenceAnimation(a => { })
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => a.MakeDefaultAnimation())
                        .AddAnimation("baz", a => { }));

            _ = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.FXController.parameters[0].defaultBool, Is.True);
            Assert.That(testSetup.Assets.FXController.parameters[1].defaultBool, Is.False);
            Assert.That(testSetup.Assets.FXController.parameters[2].defaultInt, Is.EqualTo(2));

            Assert.That(testSetup.Assets.Parameters.parameters[0].defaultValue, Is.EqualTo(1f));
            Assert.That(testSetup.Assets.Parameters.parameters[1].defaultValue, Is.EqualTo(0f));
            Assert.That(testSetup.Assets.Parameters.parameters[2].defaultValue, Is.EqualTo(2f));
        }

        [Test]
        public void UsesRootMenu_IfMenuPathIsNullOrEmptyOrSlash()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath(null)
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath(string.Empty)
                        .AddAnimation("bar", a => { }))
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath("/")
                        .AddAnimation("baz", a => { }));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(result.GeneratedMenus, HasCountConstraint.Create(0));
            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo("foo"));
            Assert.That(testSetup.Assets.Menu.controls[1].name, Is.EqualTo("bar"));
            Assert.That(testSetup.Assets.Menu.controls[2].name, Is.EqualTo("baz"));
        }

        [Test]
        public void CreatesSubmenus_IfNotPreExisting()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath("1/foo")
                        .AddAnimation("foo", a => { }));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(result.GeneratedMenus, HasCountConstraint.Create(2));
            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo("1"));
            Assert.That(testSetup.Assets.Menu.controls[0].subMenu.controls[0].name, Is.EqualTo("foo"));
        }

        [Test]
        public void DoesNotCreateSubmenus_IfSlashesAreEscaped()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath(@"\/1"))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath(@"2\/"))
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath(@"/\//3\//\/foo\//\//"));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo("/1"));
            Assert.That(testSetup.Assets.Menu.controls[1].name, Is.EqualTo("2/"));

            Assert.That(testSetup.Assets.Menu.controls[2].name, Is.EqualTo("/"));
            Assert.That(testSetup.Assets.Menu.controls[2].subMenu.controls[0].name, Is.EqualTo("3/"));
            Assert.That(
                testSetup.Assets.Menu.controls[2]
                .subMenu.controls[0]
                .subMenu.controls[0].name, Is.EqualTo("/foo/"));
            Assert.That(
                testSetup.Assets.Menu.controls[2]
                .subMenu.controls[0]
                .subMenu.controls[0]
                .subMenu.controls[0].name, Is.EqualTo("/"));
        }

        [Test]
        public void MenusArePotentiallyCreated_BasedOnBackslashEscaping()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath(@"\\/1"))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath(@"2/\\"))
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath(@"3\\\\/foo"))
                .AddLayer(
                    "4",
                    l => l
                        .WithMenuPath(@"4\\\\\/foo"))
                .AddLayer(
                    "5",
                    l => l
                        .WithMenuPath(@"\foo\bar\")); //aka that backslashes not preceeding a [\\/] work fine

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo(@"\"));
            Assert.That(testSetup.Assets.Menu.controls[0].subMenu.controls[0].name, Is.EqualTo(@"1"));

            Assert.That(testSetup.Assets.Menu.controls[1].name, Is.EqualTo(@"2"));
            Assert.That(testSetup.Assets.Menu.controls[1].subMenu.controls[0].name, Is.EqualTo(@"\"));

            Assert.That(testSetup.Assets.Menu.controls[2].name, Is.EqualTo(@"3\\"));
            Assert.That(testSetup.Assets.Menu.controls[2].subMenu.controls[0].name, Is.EqualTo(@"foo"));

            Assert.That(testSetup.Assets.Menu.controls[3].name, Is.EqualTo(@"4\\/foo"));

            Assert.That(testSetup.Assets.Menu.controls[4].name, Is.EqualTo(@"\foo\bar\"));
        }

        [Test]
        public void SubmenuGenerationWorks_WhenRedundantSlashesArePresent()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath("1/"))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath("/2"))
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath("////////////3////////////////foo/////bar/////"));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo("1"));
            Assert.That(testSetup.Assets.Menu.controls[1].name, Is.EqualTo("2"));
            Assert.That(testSetup.Assets.Menu.controls[2].name, Is.EqualTo("3"));
            Assert.That(
                testSetup.Assets.Menu.controls[2]
                .subMenu.controls[0].name, Is.EqualTo("foo"));
            Assert.That(
                testSetup.Assets.Menu.controls[2]
                .subMenu.controls[0]
                .subMenu.controls[0].name, Is.EqualTo("bar"));
        }

        [Test]
        public void MenuControlNamesCanBeChanged_WhenMenuNameOverrideIsSet()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .AddAnimation("foo", a => a.WithToggleName("bar")));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Menu.controls[0].name, Is.EqualTo("bar"));
        }

        [Test]
        public void NewSubMenusNotCreated_WhenMenuAlreadyExists()
        {
            TestSetup testSetup = new TestSetup();
            testSetup.Assets.Menu.controls.Add(new VRCExpressionsMenu.Control()
            {
                name = "foo",
                type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                subMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>()
            });
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .WithMenuPath("foo/bar"));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(result.GeneratedMenus, HasCountConstraint.Create(1));
            Assert.That(testSetup.Assets.Menu.controls[0].subMenu.controls[0].name, Is.EqualTo("bar"));
        }

        [Test]
        public void NoTogglesCreated_WhenOnlyReferenceAnimation()
        {
            //which is virtually useless, at least at the time of wrinting, having an always-on animation
            //as such, we dont expose a way to change a toggle on the reference animation via configuration builder
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .ConfigureReferenceAnimation("foo", a => { }));

            EZFXLayerConfiguration config = testSetup.ConfigurationBuilder.Generate();
            config.Layers[0].referenceAnimation.toggleNameOverride = "shouldnotexist";
            EZFXLayerGenerator generator = new EZFXLayerGenerator(config);
            GenerationResult result = generator.Generate(testSetup.Avatars, testSetup.Assets);

            Assert.That(testSetup.Assets.Menu.controls, HasCountConstraint.Create(0));
        }

        [Test]
        public void GenerationResult_IncludesCorrectMenusAndPaths_WhenGenerated()
        {
            //which is virtually useless, at least at the time of wrinting, having an always-on animation
            //as such, we dont expose a way to change a toggle on the reference animation via configuration builder
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .WithMenuPath(@"foo/\/bar/\\baz")
                        .AddAnimation("anim", a => { }));

            GenerationResult result = testSetup.StandardGenerate();

            Assert.That(result.GeneratedMenus, HasCountConstraint.Create(3));

            //note that the foo control is in root menu, which already has a test
            Assert.That(result.GeneratedMenus[0].Menu.controls[0].name, Is.EqualTo(@"/bar"));
            Assert.That(result.GeneratedMenus[0].PathComponents, Is.EqualTo(new[] { @"foo" }));

            Assert.That(result.GeneratedMenus[1].Menu.controls[0].name, Is.EqualTo(@"\baz"));
            Assert.That(result.GeneratedMenus[1].PathComponents, Is.EqualTo(new[] { @"foo", @"/bar" }));

            Assert.That(result.GeneratedMenus[2].Menu.controls[0].name, Is.EqualTo(@"anim"));
            Assert.That(result.GeneratedMenus[2].PathComponents, Is.EqualTo(new[] { @"foo", @"/bar", @"\baz" }));
        }
    }
}
