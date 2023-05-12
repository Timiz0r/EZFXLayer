namespace EZUtils.EZFXLayer.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using EZUtils.Localization.UIElements;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    using static Localization;

    [CustomEditor(typeof(ReferenceComponent))]
    public class ReferenceComponentEditor : Editor
    {
        [SerializeField] private VisualTreeAsset uxml;

        private ReferenceComponent Target => (ReferenceComponent)target;

        public Scene TargetScene => Target.gameObject.scene;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement element = uxml.CommonUIClone();
            TranslateElementTree(element);

            element.Q<Toolbar>().AddLocaleSelector();

            element.Q<LayerCreationButtons>().SetTarget(Target.gameObject);

            Button createBasicFXControllerButton = element.Q<Button>(name: "createBasicFXLayerController");
            createBasicFXControllerButton.clicked += () =>
            {
                CreateBasicFXLayerController(Target, TargetScene);
                serializedObject.Update();
            };

            Button createBasicMenuButton = element.Q<Button>(name: "createBasicVRCRootExpressionsMenu");
            createBasicMenuButton.clicked += () =>
            {
                CreateBasicVRCRootExpressionsMenu(Target, TargetScene);
                serializedObject.Update();
            };

            Button createBasicParametersButton = element.Q<Button>(name: "createBasicVRCExpressionParameters");
            createBasicParametersButton.clicked += () =>
            {
                CreateBasicVRCExpressionParameters(Target, TargetScene);
                serializedObject.Update();
            };

            Button populateFromFirstAvatarButton = element.Q<Button>(name: "populateFromFirstAvatar");
            populateFromFirstAvatarButton.clicked += () =>
            {
                PopulateFromFirstAvatarInScene(Target, TargetScene);
                serializedObject.Update();
            };

            ObjectField fxControllerField = element.Q<ObjectField>(name: "fxControllerField");
            ObjectField menuField = element.Q<ObjectField>(name: "menuField");
            ObjectField parametersField = element.Q<ObjectField>(name: "parametersField");


            //since the component isnt bound yet, we gotta schedule this ahead a frame
            //assuming it binds without notify the first time, hence the problem
            _ = element.schedule.Execute(() =>
            {
                UIValidator fxControllerCreateValidator = new UIValidator();
                fxControllerCreateValidator.AddValueValidation(fxControllerField, passCondition: v => v == null);
                fxControllerCreateValidator.DisableIfInvalid(createBasicFXControllerButton);

                UIValidator menuCreateValidator = new UIValidator();
                menuCreateValidator.AddValueValidation(menuField, passCondition: v => v == null);
                menuCreateValidator.DisableIfInvalid(createBasicMenuButton);

                UIValidator parametersCreateValidator = new UIValidator();
                parametersCreateValidator.AddValueValidation(parametersField, passCondition: v => v == null);
                parametersCreateValidator.DisableIfInvalid(createBasicParametersButton);

                UIValidator populateFromFirstAvatarValidator = new UIValidator();
                populateFromFirstAvatarValidator.AddValueValidation(fxControllerField, passCondition: v => v == null);
                populateFromFirstAvatarValidator.AddValueValidation(menuField, passCondition: v => v == null);
                populateFromFirstAvatarValidator.AddValueValidation(parametersField, passCondition: v => v == null);
                populateFromFirstAvatarValidator.DisableIfInvalid(populateFromFirstAvatarButton);
            });

            element.Q<Button>(name: "generate").clicked += () =>
            {
                IEnumerable<AnimatorLayerComponent> layers = GetComponentsInScene<AnimatorLayerComponent>();
                IEnumerable<VRCAvatarDescriptor> avatars = GetComponentsInScene<VRCAvatarDescriptor>();
                GeneratorRunner runner = new GeneratorRunner(Target, layers, avatars);
                runner.Generate();
            };

            return element;
        }

        [MenuItem("GameObject/Enable EZFXLayer in Scene", isValidateFunction: false, 20)]
        private static void EnableInScene()
        {
            ReferenceComponent[] existingComponents = FindObjectsOfType<ReferenceComponent>();
            if (existingComponents.Length > 0)
            {
                EditorError.Display(T($"EZFXLayer is already enabled through GameObject '{existingComponents[0].name}'."));
                return;
            }

            using (UndoGroup undoGroup = new UndoGroup(T("Enable EZFXLayer in Scene")))
            {
                GameObject referenceObject = GameObject.Find("EZFXLayer");
                if (referenceObject == null)
                {
                    referenceObject = new GameObject("EZFXLayer");
                    Undo.RegisterCreatedObjectUndo(referenceObject, T("Add new EZFXLayer object"));
                }

                ReferenceComponent referenceComponent = referenceObject.AddComponent<ReferenceComponent>();
                Scene scene = SceneManager.GetActiveScene();

                PopulateFromFirstAvatarInScene(referenceComponent, scene);

                if (referenceComponent.fxLayerController == null)
                {
                    CreateBasicFXLayerController(referenceComponent, scene);
                }
                if (referenceComponent.vrcExpressionParameters == null)
                {
                    CreateBasicVRCExpressionParameters(referenceComponent, scene);
                }
                if (referenceComponent.vrcRootExpressionsMenu == null)
                {
                    CreateBasicVRCRootExpressionsMenu(referenceComponent, scene);
                }
            }
        }

        private static void PopulateFromFirstAvatarInScene(ReferenceComponent referenceComponent, Scene scene)
        {
            //we do first avatar instead of selected avatar because it's easier for the user
            //and this is likely just for initial setup. otherwise, would need to lock the inspector
            //though could allow dragging and dropping into an object field to popupate
            VRCAvatarDescriptor firstAvatar = GetComponentsInScene<VRCAvatarDescriptor>(scene).First();

            Utilities.RecordChange(referenceComponent, T("Populate reference configuration from first avatar"), target =>
            {
                target.fxLayerController = firstAvatar.baseAnimationLayers
                    .FirstOrDefault(l => l.type == VRCAvatarDescriptor.AnimLayerType.FX)
                    .animatorController;
                if (target.fxLayerController == null)
                {
                    Debug.LogWarning(T($"The avatar '{firstAvatar.gameObject.name}' has no FX layer."));
                }

                target.vrcExpressionParameters = firstAvatar.expressionParameters;
                if (target.vrcExpressionParameters == null)
                {
                    Debug.LogWarning(T($"The avatar '{firstAvatar.gameObject.name}' has no expression parameters."));
                }

                target.vrcRootExpressionsMenu = firstAvatar.expressionsMenu;
                if (target.vrcRootExpressionsMenu == null)
                {
                    Debug.LogWarning(T($"The avatar '{firstAvatar.gameObject.name}' has no expressions menu."));
                }
            });
        }

        private static void CreateBasicVRCRootExpressionsMenu(ReferenceComponent referenceComponent, Scene scene)
        {
            VRCExpressionsMenu menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();

            AssetDatabase.CreateAsset(menu, GenerateSceneBasedPath(scene, s => $"ExpressionsMenu_{s.name}.asset"));

            Utilities.RecordChange(
                referenceComponent,
                T("Configure default VRC root expressions menu"),
                t => t.vrcRootExpressionsMenu = menu);
        }

        private static void CreateBasicVRCExpressionParameters(ReferenceComponent referenceComponent, Scene scene)
        {
            VRCExpressionParameters parameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            parameters.parameters = new[]
            {
                new VRCExpressionParameters.Parameter()
                {
                    defaultValue = 0,
                    name = "VRCEmote",
                    saved = true,
                    valueType = VRCExpressionParameters.ValueType.Int
                },
                new VRCExpressionParameters.Parameter()
                {
                    defaultValue = 0,
                    name = "VRCFaceBlendH",
                    saved = true,
                    valueType = VRCExpressionParameters.ValueType.Float
                },
                new VRCExpressionParameters.Parameter()
                {
                    defaultValue = 0,
                    name = "VRCFaceBlendH",
                    saved = true,
                    valueType = VRCExpressionParameters.ValueType.Float
                }
            };

            AssetDatabase.CreateAsset(
                parameters, GenerateSceneBasedPath(scene, s => $"ExpressionParameters_{s.name}.asset"));

            Utilities.RecordChange(
                referenceComponent,
                T("Configure default VRC expression parameters"),
                t => t.vrcExpressionParameters = parameters);
        }

        private static void CreateBasicFXLayerController(ReferenceComponent referenceComponent, Scene scene)
            => Utilities.RecordChange(
                referenceComponent,
                T("Configure default FX layer animator controller"),
                t =>
                {
                    VrcDefaultAnimatorControllers controllers = new VrcDefaultAnimatorControllers();

                    AnimatorControllerLayer[] layers = controllers.FX.layers;
                    layers[0].avatarMask = null;
                    controllers.FX.layers = layers;

                    t.fxLayerController = controllers.FX;
                    AssetDatabase.CreateAsset(controllers.FX, GenerateSceneBasedPath(scene, s => $"FX_{s.name}.controller"));
                });

        private static string GenerateSceneBasedPath(Scene scene, Func<Scene, string> fileNameGenerator)
        {
            string result = $"{Path.GetDirectoryName(scene.path)}/{fileNameGenerator(scene)}";
            return result;
        }

        private IEnumerable<T> GetComponentsInScene<T>(bool includeInactive = false) where T : Component
            => GetComponentsInScene<T>(TargetScene, includeInactive);
        private static IEnumerable<T> GetComponentsInScene<T>(Scene scene, bool includeInactive = false) where T : Component
            => scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(includeInactive: includeInactive));
    }
}
