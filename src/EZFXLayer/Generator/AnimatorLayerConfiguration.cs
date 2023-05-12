namespace EZUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    using static Localization;

    //the reason AnimationConfigurationHelper exists and AnimatorLayerConfigurationHelper doesnt is because
    //this class is a public driver port of the app, and the other is an internal implementation detail of the app
    public class AnimatorLayerConfiguration
    {
        private readonly IReadOnlyList<AnimationConfigurationHelper> animations;
        private readonly IProcessedParameter parameter;
        private readonly string menuPath;
        private readonly bool manageAnimatorControllerStates;
        private readonly bool manageExpressionMenuAndParameters;

        public string Name { get; }

        public bool IsMarkerLayer { get; }

        public AnimatorLayerConfiguration(
            string name,
            IReadOnlyList<AnimationConfiguration> animations,
            string menuPath,
            bool manageAnimatorControllerStates,
            bool manageExpressionMenuAndParameters,
            bool saveExpressionParameters,
            GenerationOptions generationOptions)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            this.animations = animations?
                .Select(a => new AnimationConfigurationHelper(a, generationOptions))?
                .ToArray()
                ?? throw new ArgumentNullException(nameof(animations));
            this.menuPath = menuPath;
            this.manageAnimatorControllerStates = manageAnimatorControllerStates;
            this.manageExpressionMenuAndParameters = manageExpressionMenuAndParameters;
            //so if default, i will be 0 -- which corresponds to the reference animation and is what we want
            int referenceAnimationIndex = -1;
            int defaultAnimationIndex = -1;
            for (int i = 0; i < animations.Count; i++)
            {
                AnimationConfiguration animation = animations[i];
                if (animation.isDefaultAnimation)
                {
                    defaultAnimationIndex = i;
                }
                if (animation.isReferenceAnimation)
                {
                    referenceAnimationIndex = i;
                }
            }
            if (referenceAnimationIndex == -1) throw new ArgumentOutOfRangeException(
                nameof(animations), T("There is no reference animation."));
            int defaultValue = defaultAnimationIndex >= 0 ? defaultAnimationIndex : referenceAnimationIndex;

            parameter = animations.Count > 2
                ? (IProcessedParameter)new IntProcessedParameter(name, defaultValue, saveExpressionParameters)
                : new BooleanProcessedParameter(name, defaultValue != 0, saveExpressionParameters);

            IsMarkerLayer = animations.All(a => a.gameObjects.Count == 0 && a.blendShapes.Count == 0);
        }


        public static AnimatorLayerConfiguration FromComponent(AnimatorLayerComponent layer, GenerationOptions generationOptions)
            => layer != null
                ? new AnimatorLayerConfiguration(
                    name: layer.name,
                    animations: layer.animations.Prepend(layer.referenceAnimation).ToArray(),
                    menuPath: layer.menuPath,
                    manageAnimatorControllerStates: layer.manageAnimatorControllerStates,
                    manageExpressionMenuAndParameters: layer.manageExpressionMenuAndParameters,
                    saveExpressionParameters: layer.saveExpressionParameters,
                    generationOptions: generationOptions ?? throw new ArgumentNullException(nameof(generationOptions)))
                : throw new ArgumentNullException(nameof(layer));

        internal void EnsureLayerExistsInController(
            AnimatorController controller,
            string previousLayerName,
            IAssetRepository assetRepository)
        {
            List<AnimatorControllerLayer> layers = new List<AnimatorControllerLayer>(controller.layers);
            if (layers.Any(l => l.name.Equals(Name, StringComparison.OrdinalIgnoreCase))) return;

            AnimatorControllerLayer animatorLayer = new AnimatorControllerLayer()
            {
                blendingMode = AnimatorLayerBlendingMode.Override,
                name = Name,
                stateMachine = new AnimatorStateMachine()
                {
                    name = Name,
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

        internal void PerformStateManagement(AnimatorController controller, IAssetRepository assetRepository)
        {
            if (!manageAnimatorControllerStates) return;

            AnimatorStateMachine stateMachine = GetStateMachine(controller);
            //not using ChildAnimatorState mainly because we're assuming it's okay to reposition them in
            //an orderly fashion if we're allowed to add new states. at least until there's some scenario presented
            //where we shouldnt.
            //therefore, we can ditch them!
            List<AnimatorState> states = new List<AnimatorState>(stateMachine.states.Select(cs => cs.state));

            //the main reason for generating this array is for convenience of logging and removing the subassets
            //without creating a bunch of undo operations from AnimatorStateMachine.RemoveState.
            //otherwise we could just filter out from states what's not in animations
            AnimatorState[] statesToRemove = states
                .Where(s => !animations.Any(a => a.MatchesState(s)))
                .ToArray();
            foreach (AnimatorState state in statesToRemove)
            {
                _ = states.Remove(state);
                //this doesnt seem to need to be checked for being a sub asset or not, based on passing unit test
                assetRepository.FXAnimatorControllerStateRemoved(state);
                Debug.LogWarning(
                    T($"The animator state '{state.name}' of controller layer '{Name}' exists in the base animator controller '{controller.name}' but has no corresponding animation set. ") +
                    T("Consider removing the state from the controller. It has been removed from the generated " +
                    "controller automatically, but not the base one. This has been done because we replace all " +
                    "of the transitions' conditions and have no way to know how to create them for unknown states."));
                //not sure if we should remove transitions from asset, but it's not the end of the world to not
                //even if they get left behind
            }

            AnimatorState defaultState = null;
            foreach (AnimationConfigurationHelper animation in animations)
            {
                animation.AddState(states, ref defaultState, assetRepository);
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
            for (int i = 0; i < animations.Count; i++)
            {
                AnimationConfigurationHelper animation = animations[i];
                AnimatorCondition condition = parameter.GetAnimatorCondition(i);
                animation.SetTransition(stateMachine, condition, assetRepository);
            }

            parameter.ApplyToControllerParameters(controller);
        }

        internal void UpdateStatesWithClips(AnimatorController controller, IAssetRepository assetRepository)
        {
            AnimatorStateMachine stateMachine = GetStateMachine(controller);

            foreach (AnimationConfigurationHelper animation in animations)
            {
                if (animation.SetMotion(stateMachine, Name, out GeneratedClip clip))
                {
                    assetRepository.AnimationClipAdded(clip);
                }
            }
        }

        internal void PerformExpressionsManagement(
            VRCExpressionsMenu vrcRootExpressionsMenu,
            VRCExpressionParameters vrcExpressionParameters,
            IAssetRepository assetRepository)
        {
            if (!manageExpressionMenuAndParameters) return;

            VRCExpressionParameters.Parameter expressionParameter =
                parameter.ApplyToExpressionParameters(vrcExpressionParameters);

            VRCExpressionsMenu targetMenu = Utilities.FindOrCreateTargetMenu(
                vrcRootExpressionsMenu, menuPath, assetRepository);

            for (int i = 0; i < animations.Count; i++)
            {
                AnimationConfigurationHelper animation = animations[i];
                VRCExpressionsMenu.Control toggle = animation.GetMenuToggle(expressionParameter.name, i);
                if (toggle == null) continue;
                if (targetMenu.controls.Count >= 8) throw new InvalidOperationException(
                        T("Cannot add a new toggle because there are already 8 items in the menu.") +
                        T($"Menu: {menuPath}"));
                targetMenu.controls.Add(toggle);
            }
        }

        private AnimatorStateMachine GetStateMachine(AnimatorController controller)
            => controller.layers.Single(l => l.name.Equals(Name, StringComparison.OrdinalIgnoreCase)).stateMachine;
    }
}
