namespace EZFXLayer.UIElements
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using VRC.SDK3.Avatars.ScriptableObjects;

    [CustomEditor(typeof(ReferenceComponent))]
    public class ReferenceComponentEditor : Editor
    {
        private ReferenceComponent Target => (ReferenceComponent)target;

        public void CreateBasicFXLayerController()
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
        }

        public void CreateBasicVRCExpressionParameters()
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
        }

        public void CreateBasicVRCRootExpressionsMenu()
        {
            VRCExpressionsMenu menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            AssetDatabase.CreateAsset(menu, GenerateSceneBasedPath(s => $"ExpressionsMenu_{s.name}.asset"));

            Utilities.RecordChange(
                Target, "Configure default VRC root expressions menu", t => t.vrcRootExpressionsMenu = menu);
        }

        public override VisualElement CreateInspectorGUI()
        {
            ViewModelVisualElement visualElement = new ViewModelVisualElement()
            {
                ViewModel = this
            };
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/ReferenceComponentEditor.uxml");
            visualTree.CloneTree(visualElement);

            return visualElement;
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
    }
}
