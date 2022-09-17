namespace EZFXLayer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
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
            AnimatorController generatedController;
            VRCExpressionsMenu generatedMenu;
            VRCExpressionParameters generatedParameters;

            (
                generatedController,
                generatedMenu,
                generatedParameters
            ) = GenerateImpl();

            foreach (VRCAvatarDescriptor avatar in avatars)
            {
                //TODO: undo. can we generate a mega cross-target undo?
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

        private (AnimatorController, VRCExpressionsMenu, VRCExpressionParameters) GenerateImpl()
        {
            IEnumerable<AnimatorLayerConfiguration> layers =
                layerComponents.Select(l => AnimatorLayerConfiguration.FromComponent(l));
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

            (AnimatorController, VRCExpressionsMenu, VRCExpressionParameters) result = assetRepository.FinalizeAssets();
            return result;
        }
    }
}
