namespace EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.ScriptableObjects;

    internal class ProcessedLayer
    {
        private readonly string previousLayerName;
        private readonly string name;
        private readonly IEnumerable<ProcessedAnimation> animations;
        private readonly IProcessedParameter parameter;
        private readonly string menuPath;

        public ProcessedLayer(
            string name,
            string previousLayerName,
            IEnumerable<ProcessedAnimation> animations,
            IProcessedParameter parameter,
            string menuPath)
        {
            this.name = name;
            this.previousLayerName = previousLayerName;
            this.animations = animations;
            this.parameter = parameter;
            this.menuPath = menuPath;
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
            Utilities.TryAddObjectToAsset(animatorLayer.stateMachine, controller);
        }

        public void PerformStateManagement(AnimatorController controller)
        {
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
                AssetDatabase.RemoveObjectFromAsset(state);
                Debug.LogWarning(
                    $"The animator state '{state.name}' of controller layer '{name}' exists in the " +
                    $"base animator controller '{controller.name}' but has no corresponding animation set. " +
                    "Consider removing the state from the controller. It has been removed from the generated " +
                    "controller automatically, but not the base one. This has been done because we replace all " +
                    "of the transitions' conditions and have no way to know how to create them for unknown states.");
                //not sure if we should remove transitions from asset, but it's not the end of the world to not
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
                AnimatorCondition condition = parameter.GetAnimatorCondition(animation);
                animation.SetTransition(stateMachine, condition, controller);
            }

            parameter.ApplyToControllerParameters(controller);
        }

        public IReadOnlyList<GeneratedClip> UpdateStatesWithClips(
            AnimatorController controller)
        {
            List<GeneratedClip> generatedClips = new List<GeneratedClip>();
            AnimatorStateMachine stateMachine = GetStateMachine(controller);

            foreach (ProcessedAnimation animation in animations)
            {
                if (animation.SetMotion(stateMachine, name, out GeneratedClip clip))
                {
                    generatedClips.Add(clip);
                }
            }

            return generatedClips;
        }

        public IReadOnlyList<VRCExpressionsMenu> PerformExpressionsManagement(
            VRCExpressionsMenu vrcRootExpressionsMenu,
            VRCExpressionParameters vrcExpressionParameters)
        {
            VRCExpressionParameters.Parameter expressionParameter =
                parameter.ApplyToExpressionParameters(vrcExpressionParameters);

            List<VRCExpressionsMenu> createdMenus = new List<VRCExpressionsMenu>();
            VRCExpressionsMenu targetMenu = Utilities.FindOrCreateTargetMenu(
                vrcRootExpressionsMenu, menuPath, createdMenus);

            foreach (ProcessedAnimation animation in animations)
            {
                if (animation.IsToBeDefaultState) continue;
                targetMenu.controls.Add(animation.GetMenuToggle(expressionParameter));
            }

            return createdMenus;
        }

        private AnimatorStateMachine GetStateMachine(AnimatorController controller)
            => controller.layers.Single(l => l.name.Equals(name, StringComparison.OrdinalIgnoreCase)).stateMachine;
    }
}
