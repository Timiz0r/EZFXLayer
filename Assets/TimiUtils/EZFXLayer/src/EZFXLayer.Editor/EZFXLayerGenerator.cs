namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    public class EZFXLayerGenerator
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
                previousLayer = layer;
            }

            foreach (AnimatorLayerConfiguration layer in configuration.Layers)
            {
                //even if not messing with layers, states and transitions,
                //we'll still put animations in if we get a match
                //
                //for animations, we'll simply generate them. we'll leave it to the driver adapter to create the assets
                ClipManifest clipManifest = GenerateAnimations(layer);
                generatedClips.AddRange(clipManifest.Clips);

                UpdateStatesWithClips(fxLayerAnimatorController, layer, clipManifest);

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

            ProcessedLayer processedLayer = new ProcessedLayer(
                name: layer.name,
                previousLayerName: previousLayerName,
                animations: processedAnimations,
                parameter: parameter);
            return processedLayer;
        }


        private static void UpdateStatesWithClips(
            AnimatorController fxLayerAnimatorController, AnimatorLayerConfiguration layer, ClipManifest clipManifest)
        {
            AnimatorControllerLayer animatorLayer =
                fxLayerAnimatorController.layers.Single(
                    l => l.name.Equals(layer.name, StringComparison.OrdinalIgnoreCase));

            foreach (AnimatorState state in animatorLayer.stateMachine.states.Select(s => s.state))
            {
                if (clipManifest.TryFind(state.name, out AnimationClip clip))
                {
                    state.motion = clip;
                }
            }
        }

        private static ClipManifest GenerateAnimations(AnimatorLayerConfiguration layer)
        {
            ClipManifest clipManifest = new ClipManifest(layer);
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

                clipManifest.Add(animation.name, clip);
            }
            return clipManifest;
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

        //it's a design choice to basically not create assets in the generator and let its driver adapter do that
        //but this is fine to do if we can, and it incidentally matches the internal code of AnimatorController
        private static void TryAddObjectToAsset(UnityEngine.Object objectToAdd, UnityEngine.Object potentialAsset)
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(potentialAsset))) return;
            AssetDatabase.AddObjectToAsset(objectToAdd, potentialAsset);
        }

        private class ProcessedLayer
        {
            private readonly string previousLayerName;
            private readonly string name;
            private readonly IEnumerable<ProcessedAnimation> animations;
            private readonly IProcessedParameter parameter;

            public ProcessedLayer(
                string name,
                string previousLayerName,
                IEnumerable<ProcessedAnimation> animations,
                IProcessedParameter parameter)
            {
                this.name = name;
                this.previousLayerName = previousLayerName;
                this.animations = animations;
                this.parameter = parameter;
            }

            public void PerformStateManagement(AnimatorController controller)
            {
                AnimatorStateMachine stateMachine = controller.layers.Single(
                    l => l.name.Equals(name, StringComparison.OrdinalIgnoreCase)).stateMachine;
                //not using ChildAnimatorState mainly because we're assuming it's okay to reposition them in
                //an orderly fashion if we're allowed to add new states. at least until there's some scenario presented
                //where we shouldnt.
                //therefore, we can ditch them!
                List<AnimatorState> states = new List<AnimatorState>(stateMachine.states.Select(cs => cs.state));

                //the main reason for generating this array is for convenience of logging and removing the subassets
                //without creating a bunch of undo operations from AnimatorStateMachine.RemoveState.
                AnimatorState[] statesToRemove = states
                    .Where(s => !animations.Any(a => a.MatchesState(s)))
                    .ToArray();
                foreach (AnimatorState state in statesToRemove)
                {
                    _ = states.Remove(state);
                    //this doesnt seem to need to be checked for being a sub asset or not, based on passing unit test
                    AssetDatabase.RemoveObjectFromAsset(state);
                    Debug.LogWarning(
                        $"The animator state '{state.name}' of controller layer '{name}' exists in the " +
                        $"base animator controller '{controller.name}' but has no corresponding animation set. " +
                        "Consider removing the state from the controller. It has been removed from the generated " +
                        "controller automatically, but not the base one. This has been done because we replace all " +
                        "of the transitions' conditions and have no way to know how to create them for unknown states.");
                    //not sure if we should remove transitions from asset, but it's not the end of the world to not to
                    //even if they get left behind
                }

                AnimatorState defaultState = null;
                foreach (ProcessedAnimation animation in animations)
                {
                    animation.AddState(controller, states, ref defaultState);
                }

                stateMachine.states = states
                    .Select((s, i) => new ChildAnimatorState()
                    {
                        state = s,
                        position = new Vector3(250, i * 100, 0)
                    })
                    .ToArray();
                //has to happen after we set back stateMachine.states
                //speculation: if referenceAnimationState is a new state, then perhaps stateMachine.defaultState does some
                //validation or otherwise has no understanding of this state.
                stateMachine.defaultState = defaultState;

                //likewise, let's just assume this should come after setting stateMachine.states
                foreach (ProcessedAnimation animation in animations)
                {
                    parameter.ApplyTransition(controller, stateMachine, animation);
                }

                parameter.ApplyToControllerParameters(controller);
            }

            public void EnsureLayerExistsInController(AnimatorController controller)
            {
                List<AnimatorControllerLayer> layers = new List<AnimatorControllerLayer>(controller.layers);
                if (layers.Any(l => l.name.Equals(name, StringComparison.OrdinalIgnoreCase))) return;

                AnimatorControllerLayer animatorLayer = new AnimatorControllerLayer()
                {
                    blendingMode = AnimatorLayerBlendingMode.Override,
                    name = name,
                    stateMachine = new AnimatorStateMachine()
                    {
                        name = name,
                        hideFlags = HideFlags.HideInHierarchy
                    },
                    defaultWeight = 1
                };

                int lastProcessedLayerIndex = layers.FindIndex(
                    0,
                    l => l.name.Equals(previousLayerName, StringComparison.OrdinalIgnoreCase));
                if (lastProcessedLayerIndex == -1)
                {
                    layers.Add(animatorLayer);
                }
                else
                {
                    //the configuration assumes the order of the components matter,
                    //since layers can obviously blend together
                    //we dont do a sort later based on AnimatorLayers because not every AnimatorControllerLayer has a
                    //corresponding animatorlayer
                    layers.Insert(lastProcessedLayerIndex + 1, animatorLayer);
                }
                controller.layers = layers.ToArray();

                //unity code does an undo record, but we dont since we're generating in a temp folder
                TryAddObjectToAsset(animatorLayer.stateMachine, controller);
            }
        }

        private class ProcessedAnimation
        {
            private readonly string stateName;
            //not a huge fan of such mutations, but it's convenient and the logic flows naturally
            private AnimatorState correspondingState = null;

            public AnimatorState CorrespondingState => correspondingState ?? throw new InvalidOperationException(
                "Somehow AddState didn't produce a CorrespondingState, or AddState wasn't called beforehand.");
            public int Index { get; }
            public bool IsToBeDefaultState { get; }

            public ProcessedAnimation(string stateName, int index, bool isToBeDefaultState)
            {
                this.stateName = stateName;
                Index = index;
                IsToBeDefaultState = isToBeDefaultState;
            }

            public bool MatchesState(AnimatorState state)
                => state.name.Equals(stateName, StringComparison.OrdinalIgnoreCase);

            public void AddState(AnimatorController controller, List<AnimatorState> states, ref AnimatorState defaultState)
            {
                correspondingState = states.SingleOrDefault(
                    s => s.name.Equals(stateName, StringComparison.OrdinalIgnoreCase));
                if (correspondingState != null) return;
                correspondingState = new AnimatorState()
                {
                    hideFlags = HideFlags.HideInHierarchy,
                    name = stateName
                };
                TryAddObjectToAsset(correspondingState, controller);
                states.Add(correspondingState);

                defaultState = IsToBeDefaultState ? correspondingState : defaultState;
            }
        }

        private interface IProcessedParameter
        {
            void ApplyToExpressionsParameters();
            void ApplyToControllerParameters(AnimatorController controller);
            void ApplyTransition(
                AnimatorController controller, AnimatorStateMachine stateMachine, ProcessedAnimation animation);
        }

        private class BooleanProcessedParameter : IProcessedParameter
        {
            private readonly string name;
            private readonly bool defaultValue;

            public BooleanProcessedParameter(string name, bool defaultValue)
            {
                this.name = name;
                this.defaultValue = defaultValue;
            }

            public void ApplyToControllerParameters(AnimatorController controller)
            {
                List<AnimatorControllerParameter> parameters =
                    new List<AnimatorControllerParameter>(controller.parameters);
                AnimatorControllerParameter parameter = GetOrAddParameter(
                    parameters, name, AnimatorControllerParameterType.Bool);
                parameter.defaultBool = defaultValue;
                controller.parameters = parameters.ToArray();
            }

            public void ApplyToExpressionsParameters() => throw new NotImplementedException();

            public void ApplyTransition(
                AnimatorController controller,
                AnimatorStateMachine stateMachine,
                ProcessedAnimation animation)
                => EZFXLayerGenerator.ApplyTransition(
                    controller,
                    stateMachine,
                    animation,
                    new AnimatorCondition()
                    {
                        mode = animation.Index != 0
                            ? AnimatorConditionMode.If //if animator param true
                            : AnimatorConditionMode.IfNot,
                        parameter = name
                    });
        }

        private class IntProcessedParameter : IProcessedParameter
        {
            private readonly string name;
            private readonly int defaultValue;

            public IntProcessedParameter(string name, int defaultValue)
            {
                this.name = name;
                this.defaultValue = defaultValue;
            }

            public void ApplyToControllerParameters(AnimatorController controller)
            {
                List<AnimatorControllerParameter> parameters =
                    new List<AnimatorControllerParameter>(controller.parameters);
                AnimatorControllerParameter parameter = GetOrAddParameter(
                    parameters, name, AnimatorControllerParameterType.Int);
                parameter.defaultInt = defaultValue;
                controller.parameters = parameters.ToArray();
            }

            public void ApplyToExpressionsParameters() => throw new NotImplementedException();
            public void ApplyTransition(
                AnimatorController controller,
                AnimatorStateMachine stateMachine,
                ProcessedAnimation animation)
                => EZFXLayerGenerator.ApplyTransition(
                    controller,
                    stateMachine,
                    animation,
                    new AnimatorCondition()
                    {
                        mode = AnimatorConditionMode.Equals,
                        parameter = name,
                        threshold = animation.Index
                    });
        }

        private static void ApplyTransition(
            AnimatorController controller,
            AnimatorStateMachine stateMachine,
            ProcessedAnimation animation,
            AnimatorCondition condition)
        {
            List<AnimatorStateTransition> transitions = new List<AnimatorStateTransition>(stateMachine.anyStateTransitions);
            AnimatorState state = animation.CorrespondingState;
            AnimatorStateTransition transition =
                transitions.FirstOrDefault(t => t.destinationState == state);
            if (transition == null)
            {
                transition = new AnimatorStateTransition()
                {
                    hasExitTime = false,
                    hasFixedDuration = true,
                    duration = 0,
                    exitTime = 0,
                    hideFlags = HideFlags.HideInHierarchy,
                    destinationState = state,
                    name = state.name //not sure if name is necessary anyway
                };
                transitions.Add(transition);
                //while we're not creating new assets here, this is okay
                TryAddObjectToAsset(transition, controller);
            }
            transition.conditions = new[] { condition };
            stateMachine.anyStateTransitions = transitions.ToArray();
        }

        private static AnimatorControllerParameter GetOrAddParameter(
            List<AnimatorControllerParameter> parameters,
            string name,
            AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter parameter =
                parameters.FirstOrDefault(p => p.name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (parameter == null)
            {
                parameter = new AnimatorControllerParameter()
                {
                    name = name,
                    //is redundant, since it set it later, but didnt check to see if AddParameter needs this
                    type = type
                };
                parameters.Add(parameter);
            }
            parameter.type = type;
            return parameter;
        }
    }
}
