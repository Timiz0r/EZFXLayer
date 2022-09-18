namespace EZUtils.EZFXLayer.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    [CustomEditor(typeof(ReferenceComponent))]
    public class ReferenceComponentEditor : Editor
    {
        [SerializeField] private VisualTreeAsset uxml;

        private ReferenceComponent Target => (ReferenceComponent)target;

        public Scene TargetScene => Target.gameObject.scene;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement element = uxml.CloneTree();

            Button createBasicFXControllerButton = element.Q<Button>(name: "createBasicFXLayerController");
            createBasicFXControllerButton.clicked += () =>
            {
                if (!CopyAsset(
                    guid: "404d228aeae421f4590305bc4cdaba16",
                    destPath: GenerateSceneBasedPath(s => $"FX_{s.name}.controller"),
                    out RuntimeAnimatorController controller))
                {
                    EditorError.Display("vrc_AvatarV3HandsLayer.controller not found.");
                    return;
                }

                Utilities.RecordChange(
                    Target, "Configure default FX layer animator controller", t => t.fxLayerController = controller);
            };

            Button createBasicMenuButton = element.Q<Button>(name: "createBasicVRCRootExpressionsMenu");
            createBasicMenuButton.clicked += () =>
            {
                VRCExpressionsMenu menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                AssetDatabase.CreateAsset(menu, GenerateSceneBasedPath(s => $"ExpressionsMenu_{s.name}.asset"));

                Utilities.RecordChange(
                    Target, "Configure default VRC root expressions menu", t => t.vrcRootExpressionsMenu = menu);
            };

            Button createBasicParametersButton = element.Q<Button>(name: "createBasicVRCExpressionParameters");
            createBasicParametersButton.clicked += () =>
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

                AssetDatabase.CreateAsset(parameters, GenerateSceneBasedPath(s => $"ExpressionParameters_{s.name}.asset"));

                Utilities.RecordChange(
                    Target, "Configure default VRC expression parameters", t => t.vrcExpressionParameters = parameters);
            };


            ObjectField fxControllerField = element.Q<ObjectField>(name: "fxControllerField");
            _ = fxControllerField.RegisterValueChangedCallback(
                evt => createBasicFXControllerButton.SetEnabled(evt.newValue == null));

            ObjectField menuField = element.Q<ObjectField>(name: "menuField");
            _ = menuField.RegisterValueChangedCallback(
                evt => createBasicMenuButton.SetEnabled(evt.newValue == null));

            ObjectField parametersField = element.Q<ObjectField>(name: "parametersField");
            _ = parametersField.RegisterValueChangedCallback(
                evt => createBasicParametersButton.SetEnabled(evt.newValue == null));

            element.Q<Button>(name: "populateFromFirstAvatar").clicked += () =>
            {
                //we do first avatar instead of selected avatar because it's easier for the user
                //and this is likely just for initial setup. otherwise, would need to lock the inspector
                //though could allow dragging and dropping into an object field to popupate
                //TODO: would also want the button to be disabled if any of the 3 object fields have a value
                //but am rushing
                VRCAvatarDescriptor firstAvatar = GetComponentsInScene<VRCAvatarDescriptor>().First();

                Utilities.RecordChange(Target, "Populate reference configuration from first avatar", target =>
                {
                    target.fxLayerController = firstAvatar.baseAnimationLayers[4].animatorController;
                    if (target.fxLayerController == null)
                    {
                        Debug.LogWarning($"The avatar '{firstAvatar.gameObject.name}' has no FX layer.");
                    }

                    target.vrcExpressionParameters = firstAvatar.expressionParameters;
                    if (target.vrcExpressionParameters == null)
                    {
                        Debug.LogWarning($"The avatar '{firstAvatar.gameObject.name}' has no expression parameters.");
                    }

                    target.vrcRootExpressionsMenu = firstAvatar.expressionsMenu;
                    if (target.vrcRootExpressionsMenu == null)
                    {
                        Debug.LogWarning($"The avatar '{firstAvatar.gameObject.name}' has no expressions menu.");
                    }
                });

                serializedObject.Update();
            };

            element.Q<Button>(name: "generate").clicked += () =>
            {
                IEnumerable<AnimatorLayerComponent> layers = GetComponentsInScene<AnimatorLayerComponent>();
                IEnumerable<VRCAvatarDescriptor> avatars = GetComponentsInScene<VRCAvatarDescriptor>();
                GeneratorRunner runner = new GeneratorRunner(Target, layers, avatars);
                runner.Generate();
            };

            return element;
        }

        private string GenerateSceneBasedPath(Func<Scene, string> fileNameGenerator)
        {
            Scene scene = Target.gameObject.scene;
            string result = $"{Path.GetDirectoryName(scene.path)}/{fileNameGenerator(scene)}";
            return result;
        }

        private static bool CopyAsset<T>(string guid, string destPath, out T asset) where T : UnityEngine.Object
        {
            //note that we use guids for asset generation because old-style vrcsdk is from a unitypackage
            //and new-style is from VCC, both of which have different paths
            destPath = AssetDatabase.GenerateUniqueAssetPath(destPath);
            string path = AssetDatabase.GUIDToAssetPath(guid);
            bool result = AssetDatabase.CopyAsset(path, destPath);

            asset = result ? AssetDatabase.LoadAssetAtPath<T>(destPath) : null;
            return result;
        }

        private IEnumerable<T> GetComponentsInScene<T>(bool includeInactive = false) where T : Component
            => TargetScene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(includeInactive: includeInactive));
    }
}
