namespace EZUtils.EZFXLayer.Test
{
    using System.Linq;
    using NUnit.Framework;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    //we'll also throw controller parameter stuff in here just because
    public class GeneratorExpressionsManagementTests
    {
        [SetUp]
        public void SetUp() => TestSetup.StandardTestSetUp();

        [Test]
        public void ChoosesAppropriateParameterType_BasedOnAnimationCount()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "1",
                l => l
                    .AddInitialAnimation(a => { })
                    .AddAnimation("1", a => { }));
            _ = testSetup.ConfigurationBuilder.AddLayer(
                "2",
                l => l
                    .AddInitialAnimation(a => { })
                    .AddAnimation("1", a => { })
                    .AddAnimation("2", a => { }));

            testSetup.StandardGenerate();

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
                        .AddInitialAnimation("foo", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .AddInitialAnimation("foo", a => { })
                        .DisableSavedParameters());

            testSetup.StandardGenerate();

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
                        .AddInitialAnimation("foo", a => { })
                        .AddAnimation("bar", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .AddInitialAnimation("foo", a => { })
                        .AddAnimation("bar", a => { })
                        .AddAnimation("baz", a => { }));

            testSetup.StandardGenerate();

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
        public void ParameterDefaultValue_IsBasedOnIsDefaultAndToggleOffAnimations()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                //for the first three, the 0th animations are toggle off animations
                //so the parameter values for each transition are based solely on the order of the animations
                .AddLayer(
                    "1",
                    l => l
                        .WithMenuPath("1")
                        .AddInitialAnimation(a => { })
                        .AddAnimation("foo", a => a.MakeDefaultAnimation()))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath("2")
                        .AddInitialAnimation(a => a.MakeDefaultAnimation())
                        .AddAnimation("foo", a => { }))
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath("3")
                        .AddInitialAnimation(a => { })
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => a.MakeDefaultAnimation())
                        .AddAnimation("baz", a => { }))
                //and these have the toggle off animations in different places
                .AddLayer(
                    "4",
                    l => l
                        .WithMenuPath("4")
                        .AddInitialAnimation(a => { })
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => a.MakeDefaultAnimation().MakeToggleOffAnimation())
                        .AddAnimation("baz", a => { }))
                .AddLayer(
                    "5",
                    l => l
                        .WithMenuPath("5")
                        .AddInitialAnimation(a => { })
                        .AddAnimation("foo", a => a.MakeToggleOffAnimation())
                        .AddAnimation("bar", a => a.MakeDefaultAnimation())
                        .AddAnimation("baz", a => { }))
                .AddLayer(
                    "6",
                    l => l
                        .WithMenuPath("6")
                        .AddInitialAnimation(a => { })
                        .AddAnimation("foo", a => { })
                        .AddAnimation("bar", a => a.MakeDefaultAnimation())
                        .AddAnimation("baz", a => a.MakeToggleOffAnimation()));

            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.FXController.parameters[0].defaultBool, Is.True);
            Assert.That(testSetup.Assets.FXController.parameters[1].defaultBool, Is.False);
            Assert.That(testSetup.Assets.FXController.parameters[2].defaultInt, Is.EqualTo(2));
            Assert.That(testSetup.Assets.FXController.parameters[3].defaultInt, Is.EqualTo(0));
            Assert.That(testSetup.Assets.FXController.parameters[4].defaultInt, Is.EqualTo(2));
            Assert.That(testSetup.Assets.FXController.parameters[5].defaultInt, Is.EqualTo(3));

            Assert.That(testSetup.Assets.Parameters.parameters[0].defaultValue, Is.EqualTo(1f));
            Assert.That(testSetup.Assets.Parameters.parameters[1].defaultValue, Is.EqualTo(0f));
            Assert.That(testSetup.Assets.Parameters.parameters[2].defaultValue, Is.EqualTo(2f));
            Assert.That(testSetup.Assets.Parameters.parameters[3].defaultValue, Is.EqualTo(0f));
            Assert.That(testSetup.Assets.Parameters.parameters[4].defaultValue, Is.EqualTo(2f));
            Assert.That(testSetup.Assets.Parameters.parameters[5].defaultValue, Is.EqualTo(3f));
        }

        [Test]
        public void ParameterSaved_BasedOnLayerSetting()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "1",
                    l => l
                        .AddInitialAnimation(a => { })
                        .AddAnimation("foo", a => a.MakeDefaultAnimation())
                        .DisableSavedParameters())
                .AddLayer(
                    "2",
                    l => l
                        .AddInitialAnimation(a => a.MakeDefaultAnimation())
                        .AddAnimation("foo", a => { }));

            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.Parameters.parameters.Single(p => p.name == "1").saved, Is.False);
            Assert.That(testSetup.Assets.Parameters.parameters.Single(p => p.name == "2").saved, Is.True);
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
                        .AddInitialAnimation("foo", a => { }))
                .AddLayer(
                    "2",
                    l => l
                        .WithMenuPath(string.Empty)
                        .AddInitialAnimation("bar", a => { }))
                .AddLayer(
                    "3",
                    l => l
                        .WithMenuPath("/")
                        .AddInitialAnimation("baz", a => { }));

            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.SubMenusAdded, HasCountConstraint.Create(0));
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
                        .AddInitialAnimation("foo", a => { }));

            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.SubMenusAdded, HasCountConstraint.Create(2));
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
                    l => l.WithMenuPath(@"\/1").AddInitialAnimation(a => { }))
                .AddLayer(
                    "2",
                    l => l.WithMenuPath(@"2\/").AddInitialAnimation(a => { }))
                .AddLayer(
                    "3",
                    l => l.WithMenuPath(@"/\//3\//\/foo\//\//").AddInitialAnimation(a => { }));

            testSetup.StandardGenerate();

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
                    l => l.WithMenuPath(@"\\/1").AddInitialAnimation(a => { }))
                .AddLayer(
                    "2",
                    l => l.WithMenuPath(@"2/\\").AddInitialAnimation(a => { }))
                .AddLayer(
                    "3",
                    l => l.WithMenuPath(@"3\\\\/foo").AddInitialAnimation(a => { }))
                .AddLayer(
                    "4",
                    l => l.WithMenuPath(@"4\\\\\/foo").AddInitialAnimation(a => { }))
                .AddLayer(
                    "5",
                    l => l.WithMenuPath(@"\foo\bar\").AddInitialAnimation(a => { })); //aka that backslashes not preceeding a [\\/] work fine

            testSetup.StandardGenerate();

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
                    l => l.WithMenuPath("1/").AddInitialAnimation(a => { }))
                .AddLayer(
                    "2",
                    l => l.WithMenuPath("/2").AddInitialAnimation(a => { }))
                .AddLayer(
                    "3",
                    l => l.WithMenuPath("////////////3////////////////foo/////bar/////").AddInitialAnimation(a => { }));

            testSetup.StandardGenerate();

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
        public void MenuControlNamesCanBeChanged_WhenCustomToggleNameIsSet()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer(
                    "layer",
                    l => l
                        .AddInitialAnimation("foo", a => a.WithToggleName("bar")));

            testSetup.StandardGenerate();

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
                    l => l.WithMenuPath("foo/bar").AddInitialAnimation(a => { }));

            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.SubMenusAdded, HasCountConstraint.Create(1));
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
                        .AddInitialAnimation("foo", a => { }));

            testSetup.StandardGenerate();

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
                        .AddInitialAnimation("anim", a => { }));

            testSetup.StandardGenerate();

            Assert.That(testSetup.Assets.SubMenusAdded, HasCountConstraint.Create(3));

            //note that the foo control is in root menu, which already has a test
            Assert.That(testSetup.Assets.SubMenusAdded[0].Menu.controls[0].name, Is.EqualTo(@"/bar"));
            Assert.That(testSetup.Assets.SubMenusAdded[0].PathComponents, Is.EqualTo(new[] { @"foo" }));

            Assert.That(testSetup.Assets.SubMenusAdded[1].Menu.controls[0].name, Is.EqualTo(@"\baz"));
            Assert.That(testSetup.Assets.SubMenusAdded[1].PathComponents, Is.EqualTo(new[] { @"foo", @"/bar" }));

            Assert.That(testSetup.Assets.SubMenusAdded[2].Menu.controls[0].name, Is.EqualTo(@"anim"));
            Assert.That(testSetup.Assets.SubMenusAdded[2].PathComponents, Is.EqualTo(new[] { @"foo", @"/bar", @"\baz" }));
        }

        [Test]
        public void Throw_IfAttemptingToToggleToFullMenu()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer("1", l => l.AddInitialAnimation("anim1", a => { }))
                .AddLayer("2", l => l.AddInitialAnimation("anim2", a => { }))
                .AddLayer("3", l => l.AddInitialAnimation("anim3", a => { }))
                .AddLayer("4", l => l.AddInitialAnimation("anim4", a => { }))
                .AddLayer("5", l => l.AddInitialAnimation("anim5", a => { }))
                .AddLayer("6", l => l.AddInitialAnimation("anim6", a => { }))
                .AddLayer("7", l => l.AddInitialAnimation("anim7", a => { }))
                .AddLayer("8", l => l.AddInitialAnimation("anim8", a => { }))
                .AddLayer("9", l => l.AddInitialAnimation("anim9", a => { }));

            Assert.That(() => testSetup.StandardGenerate(), Throws.InvalidOperationException);
        }

        [Test]
        public void Throw_IfAttemptingToAddSubmenuToFullMenu()
        {
            TestSetup testSetup = new TestSetup();
            _ = testSetup.ConfigurationBuilder
                .AddLayer("1", l => l.WithMenuPath("1").AddInitialAnimation("anim", a => { }))
                .AddLayer("2", l => l.WithMenuPath("2").AddInitialAnimation("anim", a => { }))
                .AddLayer("3", l => l.WithMenuPath("3").AddInitialAnimation("anim", a => { }))
                .AddLayer("4", l => l.WithMenuPath("4").AddInitialAnimation("anim", a => { }))
                .AddLayer("5", l => l.WithMenuPath("5").AddInitialAnimation("anim", a => { }))
                .AddLayer("6", l => l.WithMenuPath("6").AddInitialAnimation("anim", a => { }))
                .AddLayer("7", l => l.WithMenuPath("7").AddInitialAnimation("anim", a => { }))
                .AddLayer("8", l => l.WithMenuPath("8").AddInitialAnimation("anim", a => { }))
                .AddLayer("9", l => l.WithMenuPath("9").AddInitialAnimation("anim", a => { }));

            Assert.That(() => testSetup.StandardGenerate(), Throws.InvalidOperationException);
        }
    }
}
