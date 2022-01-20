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
        public bool generateOnUpload;

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

        [CustomEditor(typeof(EZFXLayerRootConfiguration))]
        public class Editor : UnityEditor.Editor
        {
            //could add a button to select from scene, but, since this is an inspector editor,
            //it would only be useful if going out of ones' way to lock it
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
                    var firstAvatar = FindObjectOfType<VRCAvatarDescriptor>();
                    if (firstAvatar is null)
                    {
                        Logger.DisplayError("No avatar could be found in the scene.");
                    }
                    else if (firstAvatar.baseAnimationLayers[4].animatorController is var fxLayerController)
                    {
                        target.FXLayerController = fxLayerController;
                    }
                    else
                    {
                        Logger.DisplayError($"The avatar '{firstAvatar.gameObject.name}' has no FX layer.");
                    }
                }

                if (GUILayout.Button("Create basic FX layer animator controller"))
                {
                    var scene = target.gameObject.scene;
                    var newAssetPath = AssetDatabase.GenerateUniqueAssetPath(
                        $"{Path.GetDirectoryName(scene.path)}/EZFXLayer_{scene.name}.controller");

                    //need to AssetManager.CopyAsseting because we cant CreateAsset a loaded asset
                    //also there are no details in testing lol
                    if (!AssetDatabase.CopyAsset(
                        "Assets/VRCSDK/Examples3/Animation/Controllers/vrc_AvatarV3HandsLayer.controller",
                        newAssetPath
                    ))
                    {
                        Logger.DisplayError("Unable to copy 'vrc_AvatarV3HandsLayer.controller'. See log for details.");
                    }
                    else
                    {
                        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(newAssetPath);
                        target.FXLayerController = controller;
                    }
                }

                EditorGUILayout.Separator();

                target.generateOnUpload = EditorGUILayout.Toggle("Generate on upload", target.generateOnUpload);
            }
        }
    }
}
