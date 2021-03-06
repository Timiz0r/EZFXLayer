namespace TimiUtils.EZFXLayer
{
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;
    using VRC.SDK3.Avatars.ScriptableObjects;

    //note to self: considered doing it scriptableobject-based, but there are times when things being animated will only
    //be within the scene. could maybe also adding that approach in addition, but meh it's fine.

    //not sure if it's possible anyway, but would be interesting to generate it fully in-memory
    //instead of generating the asset itself.
    //but being able to look at the result in a natural way is nice, so we'll go with that even if we dont have to.

    //TODO: add a button to import to convert the fx layer
    //  will not be all-or-nothing; instead, "fail slow" and partially.
    //  dont delete animations; we just wont reference those assets.
    //  will attempt to convert as much as we go. if we cant convert all transitions, leave them in place.
    //    parameters, menus, etc. but we'll do as much as possible.

    //TODO: sdk-style csproj
    //mainly langversion=latest is just annoying
    //also can then do some unit testing
    //and could hypothetically release as dll instead of source, tho i think source will be better
    [AddComponentMenu("EZFXLayer/EZFXLayer Root Configuration")]
    public class RootConfiguration : MonoBehaviour
    {
        public RuntimeAnimatorController FXLayerController;
        public VRCExpressionParameters VRCExpressionParameters;
        public VRCExpressionsMenu VRCRootExpressionsMenu;
        public bool generateOnUpload = true;

        private void Reset()
        {
            var allComponents = FindObjectsOfType<RootConfiguration>();
            if (allComponents.Length == 1) return;

            var parentNames = string.Join(", ", allComponents.Select(c => c.gameObject.name));
            Logger.DisplayError(
                $"Only one {nameof(RootConfiguration)} should exist per scene." +
                $"Game objects with the component: {parentNames}.");
            //crashes unity if the reset is clicked. works fine on first add tho.
            //but if we bring this back, use the DisplayError overload
            // if (!EditorUtility.DisplayDialog(
            //     "EZFXLayer", $"Only one {nameof(EZFXLayerRootConfiguration)} should exist per scene.", "OK", "Undo"))
            // {
            //     DestroyImmediate(this);
            // }
        }

        [MenuItem("GameObject/Enable EZFXLayer in Scene", isValidateFunction: false, 20)]
        private static void EnableEZFXLayerInScene()
        {
            var existingComponents = FindObjectsOfType<RootConfiguration>();
            if (existingComponents.Length > 0)
            {
                Logger.DisplayError($"EZFXLayer is already enabled through GameObject '{existingComponents[0].name}'.");
                return;
            }

            var ezFXLayerObject = GameObject.Find("EZFXLayer") ?? new GameObject("EZFXLayer");
            var ezFXLayerComponent = ezFXLayerObject.AddComponent<RootConfiguration>();
            ezFXLayerComponent.PopulateReferencesFromFirstAvatarInScene();

            if (ezFXLayerComponent.FXLayerController == null)
            {
                ezFXLayerComponent.CreateBasicFXLayerController();
            }
            if (ezFXLayerComponent.VRCExpressionParameters == null)
            {
                ezFXLayerComponent.CreateBasicVRCExpressionParameters();
            }
            if (ezFXLayerComponent.VRCRootExpressionsMenu == null)
            {
                ezFXLayerComponent.CreateBasicVRCRootExpressionsMenu();
            }
        }

        public void CreateBasicFXLayerController()
        {
            var scene = gameObject.scene;
            var newAssetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{Path.GetDirectoryName(scene.path)}/FX_{scene.name}.controller");

            //need to AssetManager.CopyAsseting because we cant CreateAsset a loaded asset
            //also there are no details in testing lol
            if (!AssetDatabase.CopyAsset(
                "Assets/VRCSDK/Examples3/Animation/Controllers/vrc_AvatarV3HandsLayer.controller",
                newAssetPath
            ))
            {
                Logger.DisplayError("Unable to copy 'vrc_AvatarV3HandsLayer.controller'. See log for details.");
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(newAssetPath);
            FXLayerController = controller;
        }

        private void CreateBasicVRCRootExpressionsMenu()
        {
            var scene = gameObject.scene;
            VRCRootExpressionsMenu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            AssetDatabase.CreateAsset(
                VRCRootExpressionsMenu, $"{Path.GetDirectoryName(scene.path)}/ExpressionsMenu_{scene.name}.asset");
        }

        private void CreateBasicVRCExpressionParameters()
        {
            var scene = gameObject.scene;
            VRCExpressionParameters = ScriptableObject.CreateInstance<VRCExpressionParameters>();
            AssetDatabase.CreateAsset(
                VRCExpressionParameters,
                $"{Path.GetDirectoryName(scene.path)}/ExpressionParameters_{scene.name}.asset");
        }

        public void PopulateReferencesFromFirstAvatarInScene()
        {
            var firstAvatar = FindObjectOfType<VRCAvatarDescriptor>();
            if (firstAvatar == null)
            {
                Logger.DisplayError("No avatar could be found in the scene.");
                return;
            }

            FXLayerController = firstAvatar.baseAnimationLayers[4].animatorController;
            if (FXLayerController == null)
            {
                Debug.LogWarning($"The avatar '{firstAvatar.gameObject.name}' has no FX layer.");
            }

            VRCExpressionParameters = firstAvatar.expressionParameters;
            if (VRCExpressionParameters == null)
            {
                Debug.LogWarning($"The avatar '{firstAvatar.gameObject.name}' has no expression parameters.");
            }

            VRCRootExpressionsMenu = firstAvatar.expressionsMenu;
            if (VRCRootExpressionsMenu == null)
            {
                Debug.LogWarning($"The avatar '{firstAvatar.gameObject.name}' has no expressions menu.");
            }
        }

        [CustomEditor(typeof(RootConfiguration))]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                var target = (RootConfiguration)base.target;
                EditorGUILayout.LabelField(
                    "These will serve as the reference assets when generating the FX layer animator controller, " +
                    "VRC expression parameters, and VRC expression menus for all avatars in the scene.",
                    EditorStyles.wordWrappedLabel);
                target.FXLayerController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                    target.FXLayerController, typeof(RuntimeAnimatorController), allowSceneObjects: false);
                target.VRCExpressionParameters = (VRCExpressionParameters)EditorGUILayout.ObjectField(
                    target.VRCExpressionParameters, typeof(VRCExpressionParameters), allowSceneObjects: false);
                target.VRCRootExpressionsMenu = (VRCExpressionsMenu)EditorGUILayout.ObjectField(
                    target.VRCRootExpressionsMenu, typeof(VRCExpressionsMenu), allowSceneObjects: false);

                if (GUILayout.Button("Populate from first avatar"))
                {
                    target.PopulateReferencesFromFirstAvatarInScene();
                }

                var wordWrappedButtonStyle = new GUIStyle(GUI.skin.button) { wordWrap = true };
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create basic FX layer animator controller", wordWrappedButtonStyle))
                {
                    target.CreateBasicFXLayerController();
                }
                if (GUILayout.Button("Create basic expression parameters", wordWrappedButtonStyle))
                {
                    target.CreateBasicVRCExpressionParameters();
                }
                if (GUILayout.Button("Create basic root expressions menu", wordWrappedButtonStyle))
                {
                    target.CreateBasicVRCRootExpressionsMenu();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Separator();

                EditorGUILayout.HelpBox("Auto-generation not yet working", MessageType.Info);
                target.generateOnUpload = EditorGUILayout.Toggle("Generate on upload", target.generateOnUpload);

                if (GUILayout.Button("Generate"))
                {
                    var generator = new FXLayerGenerator(target.gameObject.scene);
                    generator.Generate();
                }
            }
        }

        private static bool HasFXLayer(VRCAvatarDescriptor avatar, out RuntimeAnimatorController fxLayerController)
        {
            fxLayerController = avatar.baseAnimationLayers[4].animatorController;

            return fxLayerController != null;
        }
    }
}
