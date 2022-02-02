#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace TimiUtils.EZFXLayer
{
    public class FXLayerGenerator
    {
        //🤷
        private static readonly float ApparentFrameRate = new AnimationClip().frameRate;

        private readonly RootConfiguration rootConfiguration;
        private readonly IReadOnlyList<AnimatorLayer> animatorLayers;


        private readonly IEnumerable<VRCAvatarDescriptor> avatars;
        private readonly string baseFXLayerPath;
        private readonly string fxLayerFolder;
        private readonly string ezFXLayerRoot;
        private readonly string generationRoot;
        private readonly string workingRoot;

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
            var layersWithDuplicateNames = animatorLayers.GroupBy(l => l.layerName).Where(g => g.Count() > 1).ToArray();
            if (layersWithDuplicateNames.Length > 0)
            {
                //TODO: vaguely recall logging gameobjects allows them to be selected via the console, which would be nice
                var gameObjectsWithDuplicates = layersWithDuplicateNames
                    .SelectMany(l => l)
                    .Select(l => l.gameObject)
                    .Distinct();
                //would also be interested in logging the layer names,
                //but gameobjects more important (can also find layer name there)
                throw new Exception(
                    $"There are multiple animator layers with the same name. GameObjects: {gameObjectsWithDuplicates} ");
            }

            avatars = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<VRCAvatarDescriptor>());

            //TODO: want to do a lot of the same validations in both the generator and in the scene
            //so rather than splitting them up, just wont add any yet! 😅

            //TODO: these feel like htey need refactoring
            //also remove the side-effect from the ctor and dedupe a bit in Generate
            baseFXLayerPath = AssetDatabase.GetAssetPath(rootConfiguration.FXLayerController);
            fxLayerFolder = Path.GetDirectoryName(baseFXLayerPath);
            ezFXLayerRoot = EnsureFolderCreated(fxLayerFolder, "EZFXLayer");
            generationRoot = EnsureFolderCreated(ezFXLayerRoot, "generate");
            workingRoot = EnsureFolderCreated(ezFXLayerRoot, "working");
        }

        public void Generate()
        {
            AssetDatabase.DeleteAsset(generationRoot);
            EnsureFolderCreated(fxLayerFolder, "EZFXLayer/generate");

            var controller = TryGetAssetCopy<RuntimeAnimatorController>(baseFXLayerPath, out var c)
                ? c
                : throw new Exception(
                    $"Error copying the base FX layer controller at '{baseFXLayerPath}' to '{generationRoot}'.");
            var parameters = TryGetAssetCopy(rootConfiguration.VRCExpressionParameters, out var p, out var originalPath)
                ? p
                : throw new Exception(
                    $"Error copying the base expression parameters at '{originalPath}' to '{generationRoot}'.");
            var menu = TryGetAssetCopy(rootConfiguration.VRCRootExpressionsMenu, out var m, out originalPath)
                ? m
                : throw new Exception(
                    $"Error copying the base expression parameters at '{originalPath}' to '{generationRoot}'.");

            //ordering of both the AnimatorLayer components and the layers of the base animator controller matter
            //if there's an AnimatorLayer that doesn't have a corresponding layer in the base animator controller,
            //  we'll add that layer to the controller. but where?
            //for each AnimatorLayer, we'll attempt to find a matching layer in the controller. if we find it, we'll
            //  modify that layer. if we don't find it, we'll add a new layer directly under the layer added last
            //  (or the end if the first AnimatorLayer doesn't have a match).
            //in a sane configuration, for each layer in the base animator controller, there will be a corresponding
            //  AnimatorLayer component, and everything will be ordered as intended.

            //TODO: progress bars wooooo

            var menuBuilder = new VRCExpressionsMenuBuilder();
            AnimatorControllerLayer lastProcessedLayer = null;
            foreach (var animatorLayer in animatorLayers)
            {
                lastProcessedLayer = ProcessAnimatorLayer(
                    (AnimatorController)controller,
                    parameters,
                    menuBuilder,
                    animatorLayer,
                    lastProcessedLayer);
            }
            EditorUtility.SetDirty(controller);

            menuBuilder.Generate(menu);

            AssetDatabase.DeleteAsset(workingRoot);
            AssetDatabase.MoveAsset(generationRoot, workingRoot);

            foreach (var avatar in avatars)
            {
                avatar.customExpressions = true;
                avatar.expressionsMenu = menu;
                avatar.expressionParameters = parameters;

                avatar.customizeAnimationLayers = true;
                avatar.baseAnimationLayers[4] = new VRCAvatarDescriptor.CustomAnimLayer()
                {
                    isDefault = false,
                    type = VRCAvatarDescriptor.AnimLayerType.FX,
                    animatorController = controller
                };

                PrefabUtility.RecordPrefabInstancePropertyModifications(avatar);
            }
            EditorSceneManager.MarkSceneDirty(avatars.First().gameObject.scene);
        }

        private AnimatorControllerLayer ProcessAnimatorLayer(
            AnimatorController controller,
            VRCExpressionParameters vrcParameters,
            VRCExpressionsMenuBuilder menuBuilder,
            AnimatorLayer animatorLayer,
            AnimatorControllerLayer lastProcessedLayer)
        {
            var targetLayer = FindOrAddLayer();
            var stateMachine = targetLayer.stateMachine;

            if (animatorLayer.manageStateMachine)
            {
                AddAndRemoveStates();
                ReconfigureStateTransitions();
                //technically has nothing to do with animator state machine, but there's no point in vrc parameters
                //if we not updating state machine parameters
                GenerateVRCExpressionParameters();
                //likewise, no point in doing menus if not doing parameters
                GenerateVRCExpressionsMenu();
            }

            GenerateAnimations();

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
            //an interesting case came up where we generate a placeholder layer in the base, then rename the animatorlayer. we'd be
            //  left with the old conditions. one solution is to store additional state in the components to detect it
            //  and warn -- like layerNameAtPlaceholderGeneration. instead, since there are surely other cases where
            //  "auto-behavior" sux, we'll add a foldout that specifies what to generate:
            //  replace animations, replace conditions (and generate parameters), generate states, remove unmatched states,
            //  ~generate parameters~ (see below), generate menus (more than just toggle)
            //
            //prob will also introduce the base menu and parameters into the root configuration
            //submenus will be fun. the only peculiar case is if an animatorlayer has multiple animationsets, thereby
            //  requiring either a submenu for that animatorlayer or per-animationset configuration. will prob go with both,
            //  allowing an animationset to override the animatorlayer (probably atypical tho).
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

            return targetLayer;


            AnimatorControllerLayer FindOrAddLayer()
            {
                var layer = controller.layers.SingleOrDefault(l => l.name == animatorLayer.layerName);
                if (layer == null)
                {
                    layer = new AnimatorControllerLayer()
                    {
                        blendingMode = AnimatorLayerBlendingMode.Override,
                        name = animatorLayer.layerName,
                        stateMachine = new AnimatorStateMachine()
                        {
                            name = animatorLayer.layerName,
                            hideFlags = HideFlags.HideInHierarchy
                        },
                        defaultWeight = 1
                    };

                    var lastProcessedLayerIndex = Array.FindIndex(
                        controller.layers, l => l.name == lastProcessedLayer?.name);
                    if (lastProcessedLayerIndex == -1)
                    {
                        controller.AddLayer(layer);
                    }
                    else
                    {
                        //the configuration assumes the order of the components matter,
                        //since layers can obviously blend together
                        //we dont do a sort later based on AnimatorLayers because not every AnimatorControllerLayer has a
                        //corresponding animatorlayer
                        var newLayerSet = new List<AnimatorControllerLayer>(controller.layers);
                        newLayerSet.Insert(lastProcessedLayerIndex + 1, layer);
                        controller.layers = newLayerSet.ToArray();
                    }

                    //unity code does an undo record, but we dont since we're generating in a temp folder
                    AssetDatabase.AddObjectToAsset(layer.stateMachine, controller);
                }
                return layer;
            }

            void AddAndRemoveStates()
            {
                //not using ChildAnimatorState mainly because we're assuming it's okay to reposition them in
                //an orderly fashion if we're allowed to add new states. at least until there's some scenario presented
                //where we shouldnt.
                //therefore, we can ditch them!
                var states = new List<AnimatorState>(stateMachine.states.Select(cs => cs.state));

                //the main reason for generating this array is for convenience of logging and removing the subassets
                //without creating a bunch of undo operations from AnimatorStateMachine.RemoveState.
                var statesToRemove = states
                    .Where(s =>
                        s.name != animatorLayer.defaultAnimationSet.AnimatorStateName
                        && !animatorLayer.animationSets.Any(a => s.name == a.AnimatorStateName))
                    .ToArray();
                foreach (var state in statesToRemove)
                {
                    states.Remove(state);
                    AssetDatabase.RemoveObjectFromAsset(state);
                    Debug.LogWarning(
                        $"The animator state '{state.name}' of layer '{targetLayer.name}' exists in the " +
                        $"base animator controller '{controller.name}' but has no corresponding animation set. " +
                        "Consider removing the state from the controller. It has been removed from the generated " +
                        "controller automatically, but not the base one. This has been done because we replace all " +
                        "of the transitions' conditions and have no way to know how to create them for unknown states.");
                }

                foreach (var animationSet in animatorLayer.animationSets)
                {
                    if (states.Any(s => s.name == animationSet.AnimatorStateName)) continue;
                    states.Add(GenerateAnimatorState(animationSet));
                }

                AnimatorState defaultState = states.SingleOrDefault(
                    s => s.name == animatorLayer.defaultAnimationSet.AnimatorStateName);
                if (defaultState == null)
                {
                    states.Add(defaultState = GenerateAnimatorState(animatorLayer.defaultAnimationSet));
                }
                stateMachine.defaultState = defaultState;

                stateMachine.states = states
                    .Select(s => new ChildAnimatorState()
                    {
                        state = s,
                        position = new Vector3(250, GetYPosition(s), 0)
                    })
                    .ToArray();
                //we could do a trick where we assume the -1 only happens for defaultAnimationSet and not need
                //a ternary expression, but that'll surely backfire at some point
                float GetYPosition(AnimatorState state)
                    => state.name == animatorLayer.defaultAnimationSet.AnimatorStateName
                        ? 0
                        : (animatorLayer.animationSets.FindIndex(a => state.name == a.AnimatorStateName) + 1) * 100;
            }

            AnimatorState GenerateAnimatorState(AnimationSet animationSet)
            {
                var state = new AnimatorState()
                {
                    hideFlags = HideFlags.HideInHierarchy,
                    name = animationSet.AnimatorStateName
                };
                //feels strange to add it to the asset before adding it to the state machine,
                //but we're about to do that anyway
                AssetDatabase.AddObjectToAsset(state, controller);
                return state;
            }

            void ReconfigureStateTransitions()
            {
                var parameterType = animatorLayer.animationSets.Count > 1
                    ? AnimatorControllerParameterType.Int
                    : AnimatorControllerParameterType.Bool;
                var existingParameter = controller.parameters.FirstOrDefault(p => p.name == animatorLayer.layerName);
                if (existingParameter == null)
                {
                    //no undos, so we're okay using this method
                    controller.AddParameter(animatorLayer.layerName, parameterType);
                }
                else
                {
                    //in case of type updates, we still need to ensure it's correct, even if already existing
                    existingParameter.type = parameterType;
                }

                var transitions = new List<AnimatorStateTransition>(stateMachine.anyStateTransitions);

                foreach (var state in stateMachine.states.Select(cs => cs.state))
                {
                    var isDefaultState = state == stateMachine.defaultState;
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
                            destinationState = state,
                            name = state.name
                        };
                        transitions.Add(targetTransition);
                        AssetDatabase.AddObjectToAsset(targetTransition, controller);
                    }

                    targetTransition.conditions = new[]
                    {
                        parameterType == AnimatorControllerParameterType.Int
                            ? new AnimatorCondition()
                            {
                                mode = AnimatorConditionMode.Equals,
                                parameter = animatorLayer.layerName,
                                threshold = isDefaultState
                                    ? 0
                                    : animatorLayer.animationSets.FindIndex(
                                        anim => state.name == anim.AnimatorStateName) + 1
                            }
                            : new AnimatorCondition()
                            {
                                mode = isDefaultState
                                    ? AnimatorConditionMode.IfNot
                                    : AnimatorConditionMode.If,
                                parameter = animatorLayer.layerName
                            }
                    };
                }
                stateMachine.anyStateTransitions = transitions.ToArray();
            }

            void GenerateAnimations()
            {
                //TODO: see if we can actually use .name in a component cos it would be less error-prone
                //TODO: need path-safe naming for all the stuff we generating
                EnsureFolderCreated(generationRoot, animatorLayer.layerName);
                foreach (var animationSet in animatorLayer.animationSets.Append(animatorLayer.defaultAnimationSet))
                {

                    var clip = new AnimationClip();
                    //note that AnimationClip
                    foreach (var blendShape in animationSet.blendShapes)
                    {
                        clip.SetCurve(
                            blendShape.skinnedMeshRenderer.gameObject.GetRelativePath(),
                            typeof(SkinnedMeshRenderer),
                            $"blendShape.{blendShape.name}",
                            AnimationCurve.Constant(0, 1f / ApparentFrameRate, blendShape.value)
                        );
                    }
                    foreach (var gameObject in animationSet.gameObjects)
                    {
                        clip.SetCurve(
                            gameObject.gameObject.GetRelativePath(),
                            typeof(GameObject),
                            "m_IsActive",
                            AnimationCurve.Constant(0, 1f / ApparentFrameRate, gameObject.active ? 1f : 0f)
                        );
                    }
                    var targetState = stateMachine.states.SingleOrDefault(
                        s => s.state.name == animationSet.AnimatorStateName).state;
                    if (targetState != null)
                    {
                        //if we're not manageStateMachine, this could happen, which is perfectly fine
                        //could also not create it, but screw it why not!
                        targetState.motion = clip;
                    }
                    AssetDatabase.CreateAsset(
                        clip,
                        $"{generationRoot}/{animatorLayer.layerName}/{animatorLayer.layerName}_{animationSet.name}.anim");
                }
            }

            void GenerateVRCExpressionParameters()
            {
                var newParameters = new List<VRCExpressionParameters.Parameter>(
                    vrcParameters.parameters);
                if (newParameters.Count == 0)
                {
                    //no idea why the array is empty for fresh ones, so we'll just add in manually
                    newParameters.Add(new VRCExpressionParameters.Parameter()
                    {
                        defaultValue = 0,
                        name = "VRCEmote",
                        saved = true,
                        valueType = VRCExpressionParameters.ValueType.Int
                    });
                    newParameters.Add(new VRCExpressionParameters.Parameter()
                    {
                        defaultValue = 0,
                        name = "VRCFaceBlendH",
                        saved = true,
                        valueType = VRCExpressionParameters.ValueType.Float
                    });
                    newParameters.Add(new VRCExpressionParameters.Parameter()
                    {
                        defaultValue = 0,
                        name = "VRCFaceBlendH",
                        saved = true,
                        valueType = VRCExpressionParameters.ValueType.Float
                    });
                }

                var targetParameter = newParameters.SingleOrDefault(p => p.name == animatorLayer.layerName);
                if (targetParameter == null)
                {
                    newParameters.Add(targetParameter = new VRCExpressionParameters.Parameter()
                    {
                        name = animatorLayer.layerName,
                        defaultValue = 0,
                        saved = true
                    });
                }
                targetParameter.valueType = animatorLayer.animationSets.Count > 1
                    ? VRCExpressionParameters.ValueType.Int
                    : VRCExpressionParameters.ValueType.Bool;
                vrcParameters.parameters = newParameters.ToArray();
                EditorUtility.SetDirty(vrcParameters);
            }

            void GenerateVRCExpressionsMenu()
            {
                var parameter = vrcParameters.FindParameter(animatorLayer.layerName);
                int counter = 1; //0 is default, which doesnt need a menu
                foreach (var animationSet in animatorLayer.animationSets)
                {
                    var path = animationSet.menuPath;
                    if (string.IsNullOrEmpty(path))
                    {
                        path = animatorLayer.menuPath;
                    }
                    if (string.IsNullOrEmpty(path)) continue;

                    menuBuilder.AddEntry(
                        path,
                        animationSet.name,
                        parameter,
                        value: counter++
                    );
                }
            }
        }
        //TODO: blank animatorlayers still generate a default animation, tho seems unused
        //TODO: keep limited backups
        //TODO: create scene-based folders, since we do scene-based generation
        //TODO: an approach involving assets in some way is useful for moving between quest and pc projects, for instance
        //TODO: when a smr asset is removed and added, it's ofc a new asset. in the ui, we get nres
        //  aka should try to auto-fix it, perhaps by storing the path to the smr. for now, should at least render without nre tho.

        //we only out originalPath for logging/exception reasons
        //TODO: ofc can just throw here based on current usage
        private bool TryGetAssetCopy<T>(T original, out T asset, out string originalPath) where T : UnityEngine.Object
        {
            originalPath = AssetDatabase.GetAssetPath(original);
            return TryGetAssetCopy(originalPath, out asset);
        }
        private bool TryGetAssetCopy<T>(string path, out T asset) where T : UnityEngine.Object
        {
            var newPath = $"{generationRoot}/EZFXLayer_{Path.GetFileName(path)}";
            if (!AssetDatabase.CopyAsset(path, newPath))
            {
                asset = null;
                return false;
            }

            asset = AssetDatabase.LoadAssetAtPath<T>(newPath);
            return true;
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
#endif
