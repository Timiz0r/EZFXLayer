namespace TimiUtils.EZFXLayer
{
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    //note to self: considered doing it scriptableobject-based, but there are times when things being animated will only
    //be within the scene. could maybe also adding that approach in addition, but meh it's fine.

    //not sure if it's possible anyway, but would be interesting to generate it fully in-memory
    //instead of generating the asset itself.
    //but being able to look at the result in a natural way is nice, so we'll go with that even if we dont have to.

    //TODO: move this comment to a more appropriate place when implemented
    //not generating in place because generating a new one keeps the controller cleaner if stuff is removed later,
    //among other good reasons surely.
    public class EZFXLayerRootConfiguration : MonoBehaviour
    {
        public RuntimeAnimatorController FXLayerController;
        public bool generateOnUpload = true;

        private void Reset()
        {
            var allComponents = FindObjectsOfType<EZFXLayerRootConfiguration>();
            if (allComponents.Length == 1) return;

            var parentNames = string.Join(", ", allComponents.Select(c => c.gameObject.name));
            Logger.DisplayError(
                $"Only one {nameof(EZFXLayerRootConfiguration)} should exist per scene." +
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
            var existingComponents = FindObjectsOfType<EZFXLayerRootConfiguration>();
            if (existingComponents.Length > 0)
            {
                Logger.DisplayError($"EZFXLayer is already enabled through GameObject '{existingComponents[0].name}'.");
                return;
            }

            var ezFXLayerObject = GameObject.Find("EZFXLayer") ?? new GameObject("EZFXLayer");
            var ezFXLayerComponent = ezFXLayerObject.AddComponent<EZFXLayerRootConfiguration>();

            var firstAvatar = FindObjectOfType<VRCAvatarDescriptor>();
            if (firstAvatar == null || !HasFXLayer(firstAvatar, out _))
            {
                ezFXLayerComponent.CreateBasicFXLayerController();
            }
            else
            {
                ezFXLayerComponent.PopulateFXLayerControllerFromFirstAvatarInScene();
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

        public void PopulateFXLayerControllerFromFirstAvatarInScene()
        {
            var firstAvatar = FindObjectOfType<VRCAvatarDescriptor>();
            if (firstAvatar == null)
            {
                Logger.DisplayError("No avatar could be found in the scene.");
                return;
            }

            if (HasFXLayer(firstAvatar, out var fxLayerController))
            {
                FXLayerController = fxLayerController;
                return;
            }

            Logger.DisplayError($"The avatar '{firstAvatar.gameObject.name}' has no FX layer.");

        }

        [CustomEditor(typeof(EZFXLayerRootConfiguration))]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                var target = (EZFXLayerRootConfiguration)base.target;
                EditorGUILayout.LabelField(
                    "This animation controller will serve as the base animation controller" +
                    "when generating the FX layer for all avatars in the scene.", EditorStyles.wordWrappedLabel);
                target.FXLayerController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                    target.FXLayerController, typeof(RuntimeAnimatorController), allowSceneObjects: true);

                if (GUILayout.Button("Populate from first avatar"))
                {
                    target.PopulateFXLayerControllerFromFirstAvatarInScene();
                }

                if (GUILayout.Button("Create basic FX layer animator controller"))
                {
                    target.CreateBasicFXLayerController();
                }

                EditorGUILayout.Separator();

                target.generateOnUpload = EditorGUILayout.Toggle("Generate on upload", target.generateOnUpload);
            }
        }

        private static bool HasFXLayer(VRCAvatarDescriptor avatar, out RuntimeAnimatorController fxLayerController)
        {
            //not sure why, but `is RuntimeAnimatorController fxLayerController` lets nulls through??
            //if (avatar.baseAnimationLayers[4].animatorController is RuntimeAnimatorController fxLayerController)

            fxLayerController = avatar.baseAnimationLayers[4].animatorController;

            return fxLayerController != null;
        }
    }
}
