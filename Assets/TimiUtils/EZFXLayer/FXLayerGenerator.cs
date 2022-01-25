using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimiUtils.EZFXLayer
{
    public class FXLayerGenerator
    {
        private readonly RootConfiguration rootConfiguration;
        private readonly IReadOnlyList<AnimatorLayer> animatorLayers;
        private readonly string baseFXLayerPath;
        private readonly string fxLayerFolder;
        private readonly string basePath;
        private readonly string workingFXLayerControllerPath;

        public FXLayerGenerator(Scene scene)
        {
            var rootGameObjects = scene.GetRootGameObjects();
            var rootConfigurations = rootGameObjects.SelectMany(
                go => go.GetComponentsInChildren<RootConfiguration>()).ToArray();
            //will do most validation in Generate, but this is done here because we definitely only want one RootConfiguration
            //otherwise, we'll need a scene field (which may be better in the end 🤷)
            if (rootConfigurations.Length == 0) throw new Exception("No EZFXLayer root configuration found.");
            if (rootConfigurations.Length > 1) throw new Exception("More than one EZFXLayer root configuration found.");
            rootConfiguration = rootConfigurations[0];

            animatorLayers = rootGameObjects.SelectMany(go => go.GetComponentsInChildren<AnimatorLayer>()).ToArray();
            //or why not just validate this here, too? 🤷
            //if (animatorLayers.Select(al => al.layerName).Distinct().Count() != animatorLayers.Count)
            var layersWithDuplicateNames = animatorLayers.GroupBy(l => l.layerName).Where(g => g.Count() > 1).ToArray();
            if (layersWithDuplicateNames.Length > 0)
            {
                //TODO: vaguely recall logging gameobjects allows them to be selected via the console, which would be nice
                var gameObjectsWithDuplicates = layersWithDuplicateNames
                    .SelectMany(l => l)
                    .Select(l => l.gameObject)
                    .Distinct();
                //would also be interested in logging the layer names,
                //but gameobjects more important (can also find name there)
                throw new Exception(
                    $"There are multiple animator layers with the same name. GameObjects: {gameObjectsWithDuplicates} ");
            }

            //TODO: these feel like htey need refactoring
            //also remove the side-effect from the ctor and dedupe a bit in Generate
            baseFXLayerPath = AssetDatabase.GetAssetPath(rootConfiguration.FXLayerController);
            fxLayerFolder = Path.GetDirectoryName(baseFXLayerPath);
            basePath = EnsureFolderCreated(fxLayerFolder, "EZFXLayer/generate");
            workingFXLayerControllerPath =
                Path.Combine(basePath, $"EZFXLayer_{Path.GetFileName(baseFXLayerPath)}");
        }

        public void Generate()
        {
            AssetDatabase.DeleteAsset(basePath);
            EnsureFolderCreated(fxLayerFolder, "EZFXLayer/generate");

            var controller = GetWorkingFXLayerController();

            //ordering of both the AnimatorLayer components and the layers of the base animator controller matter
            //if there's an AnimatorLayer that doesn't have a corresponding layer in the base animator controller,
            //  we'll add that layer to the controller. but where?
            //for each AnimatorLayer, we'll attempt to find a matching layer in the controller. if we find it, we'll
            //  modify that layer. if we don't find it, we'll add a new layer directly under the layer added last
            //  (or the end if the first AnimatorLayer doesn't have a match).
            //in a sane configuration, for each layer in the base animator controller, there will be a corresponding
            //  AnimatorLayer component, and everything will be ordered as intended.

            //TODO: progress bars wooooo

            string lastProcessedLayerName = null;
            foreach (var animatorLayer in animatorLayers)
            {
                ProcessAnimatorLayer((AnimatorController)controller, animatorLayer, lastProcessedLayerName);
                lastProcessedLayerName = animatorLayer.layerName;
            }
        }

        private void ProcessAnimatorLayer(
            AnimatorController controller,
            AnimatorLayer animatorLayer,
            string lastProcessedLayerName)
        {
            var targetLayer = controller.layers.SingleOrDefault(l => l.name == animatorLayer.layerName);
            if (targetLayer == null)
            {
                targetLayer = new AnimatorControllerLayer()
                {
                    blendingMode = AnimatorLayerBlendingMode.Override,
                    name = animatorLayer.layerName,
                    stateMachine = new AnimatorStateMachine()
                    {
                        name = animatorLayer.name,
                        hideFlags = HideFlags.HideInHierarchy
                    }
                };

                var lastProcessedLayerIndex = Array.FindIndex(controller.layers, l => l.name == lastProcessedLayerName);
                if (lastProcessedLayerIndex == -1)
                {
                    controller.AddLayer(targetLayer);
                }
                else
                {
                    //the configuration assumes the order of the components matter,
                    //since layers can obviously blend together
                    var newLayerSet = new List<AnimatorControllerLayer>(controller.layers);
                    newLayerSet.Insert(lastProcessedLayerIndex, targetLayer);
                    controller.layers = newLayerSet.ToArray();
                }

                //unity code does an undo record, but we dont since we're generating in a temp folder
                AssetDatabase.AddObjectToAsset(targetLayer.stateMachine, controller);
            }

            var stateMachine = targetLayer.stateMachine;

            //removing before add because we're gonna mess with the positioning of the ChildAnimatorStates
            //and only wanna do that once
            if (animatorLayer.manageStateMachine)
            {
                //the main reason for generating this array is for convenience of logging and removing the subassets
                //without creating a bunch of undo operations from AnimatorStateMachine.RemoveState.
                var statesToRemove = stateMachine.states
                    .Select(cs => cs.state)
                    .Where(cs =>
                        cs.name != animatorLayer.defaultAnimationSet.AnimatorStateName
                        && !animatorLayer.animations.Any(a => cs.name == a.AnimatorStateName))
                    .ToArray();
                stateMachine.states = stateMachine.states
                    .Where(cs => !statesToRemove.Any(s => s == cs.state))
                    .ToArray();
                foreach (var state in statesToRemove)
                {
                    AssetDatabase.RemoveObjectFromAsset(state);
                    //TODO: should perhaps tag log lines with ezfxlayer
                    //TODO: verify these have a tostring. not playing it safe cos curious myself.
                    Debug.LogWarning(
                        $"The animator state '{state}' of layer '{targetLayer}' exists in the " +
                        $"base animator controller '{controller}' but has no corresponding animation set. " +
                        "Consider removing the state from the controller. It has been removed from the generated " +
                        "controller automatically, but not the base one.");
                }
            }

            if (animatorLayer.manageStateMachine)
            {
                //not using ChildAnimatorState mainly because we're assuming it's okay to reposition them in
                //an orderly fashion if we're allowed to add new states. at least until there's some scenario presented
                //where we shouldnt.
                var states = new List<AnimatorState>(stateMachine.states.Select(cs => cs.state));

                foreach (var animation in animatorLayer.animations)
                {
                    if (states.Any(s => s.name == animation.AnimatorStateName)) continue;
                    states.Add(GenerateAnimatorState(animation));
                }

                AnimatorState defaultState = states.SingleOrDefault(
                    s => s.name == animatorLayer.defaultAnimationSet.AnimatorStateName);
                if (defaultState == null)
                {
                    states.Add(defaultState = GenerateAnimatorState(animatorLayer.defaultAnimationSet));
                }
                stateMachine.defaultState = defaultState;

                int extraStateCounter = 0;
                stateMachine.states = states
                    .Select(s => new ChildAnimatorState()
                    {
                        state = s,
                        //TODO: see how it looks
                        position = new Vector3(100, GetYPosition(s), 0)
                    })
                    .ToArray();
                //TODO: we'll see if the non-expression version looks better
                float GetYPosition(AnimatorState state)
                    => state.name == animatorLayer.defaultAnimationSet.AnimatorStateName
                        ? 0
                        : animatorLayer.animations.FindIndex(a => state.name == a.AnimatorStateName) is var index && index == -1
                            ? --extraStateCounter * 100
                            : (index + 1) * 100;
                AnimatorState GenerateAnimatorState(AnimationSet animationSet)
                {
                    var state = new AnimatorState()
                    {
                        hideFlags = HideFlags.HideInHierarchy,
                        name = animationSet.animatorStateNameOverride ?? animationSet.name
                    };
                    //feels strange to add it to the asset before adding it to the state machine,
                    //but we're about to do that anyway
                    AssetDatabase.AddObjectToAsset(state, controller);
                    return state;
                }
            }

            //if the configuration has addMissingStates but not this, then, indeed, no transition!
            if (animatorLayer.manageStateMachine)
            {
                var parameterType = animatorLayer.animations.Count > 1
                    ? AnimatorControllerParameterType.Int
                    : AnimatorControllerParameterType.Bool;
                var existingParameter = controller.parameters.FirstOrDefault(p => p.name == animatorLayer.layerName);
                if (existingParameter == null)
                {
                    controller.AddParameter(animatorLayer.layerName, parameterType);
                }
                else
                {
                    existingParameter.type = parameterType;
                }

                var transitions = new List<AnimatorStateTransition>(stateMachine.anyStateTransitions);

                foreach (var state in stateMachine.states.Select(cs => cs.state))
                {
                    float parameterValue = animatorLayer.animations.FindIndex(
                        anim => state.name == anim.AnimatorStateName) is var index && index == -1
                            ? 0 //default state, since it's the only one not in animatorLayer.animations
                            : index + 1;

                    var targetTransition = transitions.FirstOrDefault(t => t.destinationState == state);
                    if (targetTransition == null)
                    {
                        targetTransition = new AnimatorStateTransition()
                        {
                            hasExitTime = false,
                            hasFixedDuration = true,
                            duration = 0,
                            exitTime = 0,
                            hideFlags = HideFlags.HideInHierarchy,
                            destinationState = state
                        };
                        transitions.Add(targetTransition);
                        AssetDatabase.AddObjectToAsset(targetTransition, controller);
                    }
                    targetTransition.conditions = new[] {
                        new AnimatorCondition()
                        {
                            mode = AnimatorConditionMode.If,
                            parameter = animatorLayer.layerName,
                            threshold = parameterValue
                        }
                    };
                }
            }

            //note for resuming:
            //need to think about parameters more
            //in general, want the layer name and parameter name to basically match (with some dedupe logic if conflicting).
            //let's take gestures an exception. we'll just want to fill in the animation and not worry about conditions.
            //  but what if there is a mismatch? presumably we throw and fail for this fill-in-only mode.
            //and what's the logic for deciding if to generate parameters and conditions or not? an option? or smart logic?
            //  am leaning towards an option, and we'll overwrite any existing conditions if opting into generation.
            //
            //orrrrrr maybe we have two modes: fill-in-only and generate-only.
            //fill-in-only is done if there's already a matching layer in the controller. we just supply new animations
            //  for matching states. if there's an animation without a matching state, we fail.
            //generate-only is done if there is no matching layer. we generate the animations, states, and parameters.
            //
            //an interesting case came up where we generate a placeholder layer in the base, then rename the animationlayer. we'd be
            //  left with the old conditions. one solution is to store additional state in the components to detect it
            //  and warn -- like layerNameAtPlaceholderGeneration. instead, since there are surely other cases where
            //  "auto-behavior" sux, we'll add a foldout that specifies what to generate:
            //  replace animations, replace conditions (and generate parameters), generate states, remove unmatched states,
            //  ~generate parameters~ (see below), generate menus (more than just toggle)
            //
            //prob will also introduce the base menu and parameters into the root configuration
            //submenus will be fun. the only peculiar case is if an animationlayer has multiple animationsets, thereby
            //  requiring either a submenu for that animationlayer or per-animationset configuration. will prob go with both,
            //  allowing an animationset to override the animationlayer (probably atypical tho).
            //
            //what about if we generate placeholder states, rename an animation, and arent removing unmatched states?
            //
            //furthermore, what about going from a bool generated parameter to an int?
            //  aka we gotta support this, but a weird case comes up when condition modification is off. presumably this
            //  is an unresolvable conflict that we fail on. recovery is done by reenabling it or fixing stuff manually.
            //orrrrrr, important: do we tie together parameters and conditions into the same setting? are there cases
            //  where we want them separated? presumably no -- without a transition, the animator controller has no
            //  way or reason to reference a parameters. so we'll go with that.
            //
            //okay no need for a setting for replacing animations. there's no point in this tool if not doing that.
            //
            //and a lot of weird edge cases were coming up when figuring out how all these animatorlayer settings
            //  interact. so reducing the bulk of them down to manageStateMachine, which does adding removing states and
            //  managing conditions and parameters.

            //TODO: only did parameters in the controller,  but didnt do them for vrc
            //TODO: also, still gotta do menus
        }

        private static string GetAnimationName(AnimatorLayer animatorLayer, AnimationSet animationSet)
            => $"{animatorLayer.layerName}_{animationSet.animatorStateNameOverride ?? animationSet.name}";

        private RuntimeAnimatorController GetWorkingFXLayerController()
        {
            if (!AssetDatabase.CopyAsset(baseFXLayerPath, workingFXLayerControllerPath))
            {
                throw new Exception(
                    $"Error copying the base FX layer controller at '{baseFXLayerPath}' to '{workingFXLayerControllerPath}'.");
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(workingFXLayerControllerPath);
            return controller;
        }

        private static string EnsureFolderCreated(string baseFolder, string path)
        {
            string currentPath = baseFolder;
            foreach (var pathComponent in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                var previousPath = currentPath;
                //since unity visually uses /, avoid Path.Combine in case windows
                currentPath = $"{currentPath}/{pathComponent}";
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(previousPath, pathComponent);
                }
            }
            return currentPath;
        }
    }
}
