namespace EZUtils.EZFXLayer
{
    using System;
    using System.Linq;
    using UnityEditor.Animations;
    using VRC.SDK3.Avatars.ScriptableObjects;

    using static Localization;

    public partial class EZFXLayerGenerator
    {
        private readonly EZFXLayerConfiguration configuration;

        public EZFXLayerGenerator(EZFXLayerConfiguration configuration)
        {
            this.configuration = configuration;
        }

        //TODO: thoughts on allowing the user to move the folder? it would be easiest to have it as a setting, but would
        //be interesting to somehow track the asset id of the folder.

        public void Generate(
            AnimatorController fxLayerAnimatorController,
            VRCExpressionsMenu vrcRootExpressionsMenu,
            VRCExpressionParameters vrcExpressionParameters)
        {
            PreValidate();

            if (fxLayerAnimatorController == null) throw new ArgumentNullException(nameof(fxLayerAnimatorController));
            if (vrcExpressionParameters == null) throw new ArgumentNullException(nameof(vrcExpressionParameters));
            if (vrcRootExpressionsMenu == null) throw new ArgumentNullException(nameof(vrcRootExpressionsMenu));

            AnimatorLayerConfiguration previousLayer = null;
            foreach (AnimatorLayerConfiguration layer in configuration.Layers)
            {
                layer.EnsureLayerExistsInController(
                    fxLayerAnimatorController, previousLayer?.Name, configuration.AssetRepository);
                layer.PerformStateManagement(fxLayerAnimatorController, configuration.AssetRepository);

                //even if not messing with layers, states and transitions,
                //we'll still put animations in if we get a match
                //
                //for animations, we'll simply generate them. we'll leave it to the driver adapter to create the assets
                layer.UpdateStatesWithClips(fxLayerAnimatorController, configuration.AssetRepository);

                layer.PerformExpressionsManagement(
                    vrcRootExpressionsMenu, vrcExpressionParameters, configuration.AssetRepository);

                previousLayer = layer;
            }
        }

        private void PreValidate()
        {
            string[] duplicateLayers = configuration.Layers
                .Where(l => !l.IsMarkerLayer)
                .GroupBy(l => l.Name, (name, group) => (Name: name, Count: group.Count()))
                .Where(g => g.Count > 1)
                .Select(g => g.Name)
                .ToArray();

            if (duplicateLayers.Length > 0) throw new InvalidOperationException(
                T($"Duplicate non-empty layers found in configuration: {string.Join(", ", duplicateLayers)}."));
        }
    }
}
