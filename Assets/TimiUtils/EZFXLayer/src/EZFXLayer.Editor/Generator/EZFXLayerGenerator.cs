namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public partial class EZFXLayerGenerator
    {
        private readonly EZFXLayerConfiguration configuration;

        public EZFXLayerGenerator(EZFXLayerConfiguration configuration)
        {
            this.configuration = configuration;
        }

        //TODO: one desire is to not delete still-used assets on generation like before, since it generates plenty of noise
        //like in git. this presents a problem for if failures happen, causing corruption if it happens midway.
        //ideally we can revert to the saved version somehow, such as with cleardirty? or undo?
        //at time of writing, this doesn't really apply to clips, since we're gonna take the generated clips and copy
        //the contents over to existing ones. for these parameters, gotta be careful.
        //TODO: gotta test around the case of the reference configs getting updated, since that means simply editing
        //the generated stuff in-place is insufficient
        //at this point, it's starting to seem like it would be easiest to fresh generate them every time, then manually
        //edit the guids back. would not need to be done for animations, tho, since they have no reference.
        //
        //so yeh here's the plan:
        //driver adapter will duplicate these 3 assets as usual, and we'll edit them here.
        //  and changes in the originals will always make it in this way.
        //if generation fails, no problemo nothing has actually changed. animations havent been saved either.
        //if generation succeeds
        //  we StartAssetEditing and take note of the guids of the prior generation of these 3 artifacts
        //  delete the last generated of these 3 artifacts and move the new generations in
        //  we manually file io overwrite the guids
        //  we delete all previously generation clip assets that are not part of this generation
        //  for each newly generated clip, we either find the existing one, clear it, and copy into it, or we make new
        //  StopAssetEditing
        //
        //  for finding an asset, it should be okay to go folder=layer and file=layer+animation based.
        //  however, because it sounds fun, let's make GenerationResult a ScriptableObject and save it.

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
                $"Duplicate non-empty layers found in configuration: {string.Join(", ", duplicateLayers)}.");
        }
    }
}
