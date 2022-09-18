namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine.SceneManagement;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    internal class GeneratorRunner
    {
        private readonly ReferenceComponent referenceComponent;
        private readonly IEnumerable<AnimatorLayerComponent> layerComponents;
        private readonly IEnumerable<VRCAvatarDescriptor> avatars;
        private readonly Scene scene;

        public GeneratorRunner(
            ReferenceComponent referenceComponent,
            IEnumerable<AnimatorLayerComponent> layerComponents,
            IEnumerable<VRCAvatarDescriptor> avatars)
        {
            this.referenceComponent = referenceComponent;
            this.layerComponents = layerComponents;
            this.avatars = avatars;
            scene = referenceComponent.gameObject.scene;
        }

        public void Generate()
        {
            bool exceptionCaught = false;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            try
            {
                GenerateImpl();
            }
            catch when ((exceptionCaught = true) != true) //maintain stack
            {
                //we could revert the undo group, but it may be useful for debugging to leave it
                //and the user has options, including undoing manually or fixing the issue if user-generated
                //though for finalizing assets we prefer user-generated issues to be impossible
                throw new InvalidOperationException("literally impossible but gets rid of a warning");
            }
            finally
            {
                Undo.SetCurrentGroupName(exceptionCaught ? "Failed EZFXLayer generation" : "EZFXLayer generation");
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private void GenerateImpl()
        {
            IEnumerable<AnimatorLayerConfiguration> layers =
                layerComponents.Select(l => AnimatorLayerConfiguration.FromComponent(l, referenceComponent.generationOptions));
            string outputPath = Path.Combine(Path.GetDirectoryName(scene.path), "EZFXLayer");

            EZFXLayerAssetRepository assetRepository = new EZFXLayerAssetRepository(
                outputPath,
                (AnimatorController)referenceComponent.fxLayerController,
                referenceComponent.vrcRootExpressionsMenu,
                referenceComponent.vrcExpressionParameters);
            EZFXLayerConfiguration config = new EZFXLayerConfiguration(layers.ToArray(), assetRepository);
            EZFXLayerGenerator generator = new EZFXLayerGenerator(config);

            (
                AnimatorController workingController,
                VRCExpressionsMenu workingMenu,
                VRCExpressionParameters workingParameters
            ) = assetRepository.PrepareWorkingAssets();
            generator.Generate(workingController, workingMenu, workingParameters);

            (
                AnimatorController generatedController,
                VRCExpressionsMenu generatedMenu,
                VRCExpressionParameters generatedParameters
            ) = assetRepository.FinalizeAssets();

            foreach (VRCAvatarDescriptor avatar in avatars)
            {
                //TODO: because the object reference isn't the same, even if the asset id *is* the same,
                //we still get a change -- undo, dirty scene. so let's fix this!
                Undo.RecordObject(avatar, "Applying generation to avatar");
                avatar.customExpressions = true;
                avatar.expressionsMenu = generatedMenu;
                avatar.expressionParameters = generatedParameters;

                avatar.customizeAnimationLayers = true;
                avatar.baseAnimationLayers[4] = new VRCAvatarDescriptor.CustomAnimLayer()
                {
                    isDefault = false,
                    type = VRCAvatarDescriptor.AnimLayerType.FX,
                    animatorController = generatedController
                };

                PrefabUtility.RecordPrefabInstancePropertyModifications(avatar);
            }
        }
    }
}
