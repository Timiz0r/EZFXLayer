namespace EZFXLayer.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class VrcAssetsTester
    {
        [Test]
        public void StartObjectsAreCloned_WhenResetOriginalMarkCalled()
        {
            VrcAssets assets = new VrcAssets();

            AnimatorController controller = assets.FXController;
            controller.AddParameter("controllerparamfoo", AnimatorControllerParameterType.Int);
            controller.AddLayer("controllerlayerfoo");
            _ = controller.layers[0].stateMachine.AddState("controllerstatefoo");

            assets.Menu.controls.Add(new VRCExpressionsMenu.Control() { name = "menufoo" });
            assets.Parameters.parameters = new[] { new VRCExpressionParameters.Parameter() { name = "parameterfoo" } };

            assets.ResetOriginalMark();
            assets.FXController.layers[0].stateMachine.states[0].state.name = "controllerstatebar";
            assets.Menu.controls[0].name = "menubar";
            assets.Parameters.parameters[0].name = "parameterbar";

            //will just assume that if it gets this far, then it cloned just fine
            Assert.That(assets.OriginalFXController.layers[0].stateMachine.states[0].state.name, Is.EqualTo("controllerstatefoo"));
            Assert.That(assets.OriginalMenu.controls[0].name, Is.EqualTo("menufoo"));
            Assert.That(assets.OriginalParameters.parameters[0].name, Is.EqualTo("parameterfoo"));
        }
    }
}
