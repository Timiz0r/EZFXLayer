namespace EZUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
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
                EnsureLayerExistsInController(
                    layer, fxLayerAnimatorController, previousLayer?.Name, configuration.AssetRepository);
                PerformStateManagement(layer, fxLayerAnimatorController, configuration.AssetRepository);

                //even if not messing with layers, states and transitions,
                //we'll still put animations in if we get a match
                //
                //for animations, we'll simply generate them. we'll leave it to the driver adapter to create the assets
                UpdateStatesWithClips(layer, fxLayerAnimatorController, configuration.AssetRepository);

                PerformExpressionsManagement(
                    layer, vrcRootExpressionsMenu, vrcExpressionParameters, configuration.AssetRepository);

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

        private static void EnsureLayerExistsInController(
            AnimatorLayerConfiguration layer,
            AnimatorController controller,
            string previousLayerName,
            IAssetRepository assetRepository)
        {
            List<AnimatorControllerLayer> layers = new List<AnimatorControllerLayer>(controller.layers);
            if (layers.Any(l => l.name.Equals(layer.Name, StringComparison.OrdinalIgnoreCase))) return;

            AnimatorControllerLayer animatorLayer = new AnimatorControllerLayer()
            {
                blendingMode = AnimatorLayerBlendingMode.Override,
                name = layer.Name,
                stateMachine = new AnimatorStateMachine()
                {
                    name = layer.Name,
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
            assetRepository.FXAnimatorStateMachineAdded(animatorLayer.stateMachine);
        }

        private static void PerformStateManagement(
            AnimatorLayerConfiguration layer, AnimatorController controller, IAssetRepository assetRepository)
        {
            if (!layer.ManageAnimatorControllerStates) return;

            AnimatorStateMachine stateMachine = GetStateMachine(layer, controller);
            //not using ChildAnimatorState mainly because we're assuming it's okay to reposition them in
            //an orderly fashion if we're allowed to add new states. at least until there's some scenario presented
            //where we shouldnt.
            //therefore, we can ditch them!
            List<AnimatorState> states = new List<AnimatorState>(stateMachine.states.Select(cs => cs.state));

            //the main reason for generating this array is for convenience of logging and removing the subassets
            //without creating a bunch of undo operations from AnimatorStateMachine.RemoveState.
            //otherwise we could just filter out from states what's not in animations
            AnimatorState[] statesToRemove = states
                .Where(s => !layer.Animations.Any(a => a.MatchesState(s)))
                .ToArray();
            foreach (AnimatorState state in statesToRemove)
            {
                _ = states.Remove(state);
                //this doesnt seem to need to be checked for being a sub asset or not, based on passing unit test
                assetRepository.FXAnimatorControllerStateRemoved(state);
                Debug.LogWarning(
                    T($"The animator state '{state.name}' of controller layer '{layer.Name}' exists in the base animator controller '{controller.name}' but has no corresponding animation set. ") +
                    T("Consider removing the state from the controller. It has been removed from the generated " +
                    "controller automatically, but not the base one. This has been done because we replace all " +
                    "of the transitions' conditions and have no way to know how to create them for unknown states."));
                //not sure if we should remove transitions from asset, but it's not the end of the world to not
                //even if they get left behind
            }

            AnimatorState stateMachineDefaultState = null;
            foreach (AnimationConfigurationHelper animation in layer.Animations)
            {
                animation.AddState(states, ref stateMachineDefaultState, assetRepository);
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
            stateMachine.defaultState = stateMachineDefaultState;

            //likewise, let's just assume this should come after setting stateMachine.states
            foreach (AnimationConfigurationHelper animation in layer.Animations)
            {
                AnimatorCondition condition = layer.Parameter.GetAnimatorCondition(layer.GetAnimationToggleValue(animation));
                animation.SetTransition(stateMachine, condition, assetRepository);
            }
            for (int i = 0; i < layer.Animations.Count; i++)
            {
                AnimationConfigurationHelper animation = layer.Animations[i];
            }

            layer.Parameter.ApplyToControllerParameters(controller);
        }

        private static void UpdateStatesWithClips(
            AnimatorLayerConfiguration layer, AnimatorController controller, IAssetRepository assetRepository)
        {
            AnimatorStateMachine stateMachine = GetStateMachine(layer, controller);

            foreach (AnimationConfigurationHelper animation in layer.Animations)
            {
                if (animation.SetMotion(stateMachine, layer.Name, out GeneratedClip clip))
                {
                    assetRepository.AnimationClipAdded(clip);
                }
            }
        }

        private static void PerformExpressionsManagement(
            AnimatorLayerConfiguration layer,
            VRCExpressionsMenu vrcRootExpressionsMenu,
            VRCExpressionParameters vrcExpressionParameters,
            IAssetRepository assetRepository)
        {
            if (!layer.ManageExpressionMenuAndParameters) return;

            layer.Parameter.ApplyToExpressionParameters(vrcExpressionParameters);

            VRCExpressionsMenu targetMenu = Utilities.FindOrCreateTargetMenu(
                vrcRootExpressionsMenu, layer.MenuPath, assetRepository);

            foreach (AnimationConfigurationHelper animation in layer.Animations)
            {
                int animationToggleValue = layer.GetAnimationToggleValue(animation);
                VRCExpressionsMenu.Control toggle = animation.GetMenuToggle(layer.Parameter.Name, animationToggleValue);
                if (toggle == null) continue;
                if (targetMenu.controls.Count >= 8) throw new InvalidOperationException(
                        T("Cannot add a new toggle because there are already 8 items in the menu.") +
                        T($"Menu: {layer.MenuPath}"));
                targetMenu.controls.Add(toggle);
            }
        }

        private static AnimatorStateMachine GetStateMachine(AnimatorLayerConfiguration layer, AnimatorController controller)
            => controller.layers.Single(l => l.name.Equals(layer.Name, StringComparison.OrdinalIgnoreCase)).stateMachine;
    }
}
