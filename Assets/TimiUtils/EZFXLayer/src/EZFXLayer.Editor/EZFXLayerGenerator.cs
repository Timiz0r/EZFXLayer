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
        //
        //TODO: thoughts on allowing the user to move the folder? it would be easiest to have it as a setting, but would
        //be interesting to somehow track the asset id of the folder.
        //
        //TODO: we want to allow parameters to be on by default, to keep menus consistent (FooOn, BarOff menu items).
        //we still want a reference set of animatables for consistency. we still want that to be an animation for ease
        //of use. however, we'll an an startingAnimation field -- true for the default animation. this can be turned off
        //on the default animation and on for some other animation (or perhaps better to think about it opposite).
        //but where to go from here. the easiest is to have the expression and controller param be this starting animation.
        //this will perhaps cause the default animation to get played first before the starting animation, though.
        //we'll still try this first, since it's more convenient. if it turns out bad, then perhaps we'll make the
        //default animation in the layer the starting animation. actually yeh let's try that. i suspect it'll be fine,
        //since expression parameters' saved thingy doesnt seem to result in wonky behavior, but who knows.
        //edit: this, but what's currently default animation will be reference animation, and what was to be known as
        //starting animation will be default animation. will indeed start off with having a different default param val.
        //
        //TODO: for single-animation layers, don't add animation name (so Clothes, instead of ClothesOn). could add a
        //setting to always add the animation name, but we already kinda do via the menuNameOverride setting.
        //
        //TODO: add a setting for controlling the naming format of menu items (Foo_{anim}). would be useful for cases
        //where the user wants all menu items to just be {anim}
        //TODO: actually, consider just naming things off of animation name. so, we'd for instance use ClothesOff and
        //ClothesOn instead of Off and On. perhaps simplifies everything. be sure to update tests to represent this
        //new expected usage.
        public GenerationResult Generate(
            IEnumerable<GameObject> avatars,
            AnimatorController fxLayerAnimatorController,
            VRCExpressionsMenu vrcRootExpressionsMenu,
            VRCExpressionParameters vrcExpressionParameters)
        {
            PreValidate(avatars);
            if (fxLayerAnimatorController == null) throw new ArgumentNullException(nameof(fxLayerAnimatorController));
            if (vrcExpressionParameters == null) throw new ArgumentNullException(nameof(vrcExpressionParameters));
            if (vrcRootExpressionsMenu == null) throw new ArgumentNullException(nameof(vrcRootExpressionsMenu));

            List<GeneratedClip> generatedClips = new List<GeneratedClip>();

            AnimatorLayerConfiguration previousLayer = null;
            foreach (AnimatorLayerConfiguration layer in configuration.Layers)
            {
                ProcessedLayer processedLayer = ProcessLayer(layer, previousLayerName: previousLayer?.name);

                processedLayer.EnsureLayerExistsInController(fxLayerAnimatorController);
                if (layer.manageAnimatorControllerStates)
                {
                    processedLayer.PerformStateManagement(fxLayerAnimatorController);
                }

                //even if not messing with layers, states and transitions,
                //we'll still put animations in if we get a match
                //
                //for animations, we'll simply generate them. we'll leave it to the driver adapter to create the assets
                generatedClips.AddRange(
                    processedLayer.UpdateStatesWithClips(fxLayerAnimatorController));

                previousLayer = layer;
            }

            GenerationResult result = new GenerationResult(generatedClips);
            return result;
        }

        private static ProcessedLayer ProcessLayer(AnimatorLayerConfiguration layer, string previousLayerName)
        {
            List<ProcessedAnimation> processedAnimations = new List<ProcessedAnimation>(layer.animations.Count);
            int defaultValue = 0; //reference animation/default state, incidentally
            int parameterValue = 0;
            processedAnimations.Add(
                new ProcessedAnimation(layer.referenceAnimation.EffectiveStateName, parameterValue++, isToBeDefaultState: true));
            foreach (AnimationConfiguration animation in layer.animations)
            {
                processedAnimations.Add(
                    new ProcessedAnimation(animation.EffectiveStateName, parameterValue, isToBeDefaultState: false));
                if (animation.isDefaultAnimation)
                {
                    defaultValue = parameterValue;
                }
                parameterValue++;
            }

            IProcessedParameter parameter = layer.animations.Count > 1
                ? (IProcessedParameter)new IntProcessedParameter(layer.name, defaultValue)
                : new BooleanProcessedParameter(layer.name, defaultValue != 0);

            IReadOnlyDictionary<string, AnimationClip> clips = GenerateAnimationClips(layer);

            ProcessedLayer processedLayer = new ProcessedLayer(
                name: layer.name,
                previousLayerName: previousLayerName,
                animations: processedAnimations,
                parameter: parameter,
                animationClips: clips);
            return processedLayer;
        }

        private static IReadOnlyDictionary<string, AnimationClip> GenerateAnimationClips(
            AnimatorLayerConfiguration layer)
        {
            Dictionary<string, AnimationClip> clips =
                new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
            foreach (AnimationConfiguration animation in layer.animations.Append(layer.referenceAnimation))
            {
                AnimationClip clip = new AnimationClip();
                float frameRate = clip.frameRate;

                foreach (AnimatableBlendShape blendShape in animation.blendShapes)
                {
                    clip.SetCurve(
                        blendShape.skinnedMeshRenderer.gameObject.GetRelativePath(),
                        typeof(SkinnedMeshRenderer),
                        $"blendShape.{blendShape.name}",
                        AnimationCurve.Constant(0, 1f / frameRate, blendShape.value)
                    );
                }
                foreach (AnimatableGameObject gameObject in animation.gameObjects)
                {
                    clip.SetCurve(
                        gameObject.gameObject.GetRelativePath(),
                        typeof(GameObject),
                        "m_IsActive",
                        AnimationCurve.Constant(0, 1f / frameRate, gameObject.active ? 1f : 0f)
                    );
                }

                if (clips.ContainsKey(animation.name)) throw new InvalidOperationException(
                    $"An animation named '{animation.name}' has already been generated for layer '{layer.name}'.");
                clips[animation.name] = clip;
            }
            return clips;
        }

        private void PreValidate(IEnumerable<GameObject> avatars)
        {
            //null and empty are allowed
            if (avatars == null) return;

            foreach (GameObject avatar in avatars)
            {
                if (avatar.GetComponentInChildren<VRCAvatarDescriptor>() == null)
                {
                    throw new InvalidOperationException(
                        $"Avatar '{avatar.name}' has no {nameof(VRCAvatarDescriptor)}.");
                }
            }
        }
    }
}
