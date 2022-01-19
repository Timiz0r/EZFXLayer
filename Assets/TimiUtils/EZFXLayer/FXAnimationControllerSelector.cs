namespace TimiUtils.EZFXLayer
{
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    //note to self: considered doing it scriptableobject-based, but there are times when things being animated will only be within the scene
    //could maybe also adding that approach in addition, but meh it's fine
    public class FXAnimationControllerSelector : MonoBehaviour
    {
        public VRCAvatarDescriptor targetAvatar;

        public RuntimeAnimatorController FXLayerController => targetAvatar?.baseAnimationLayers[4].animatorController;

        private void Reset()
        {
            var allComponents = FindObjectsOfType<FXAnimationControllerSelector>();
            if (allComponents.Length > 1)
            {
                var parentNames = string.Join(", ", allComponents.Select(c => c.gameObject.name));
                Debug.LogError($"Only one FXAnimationControllerSelector should exist per scene. Game objects with the component: {parentNames}.");
                //crashes unity if the reset is clicked. works fine on first add tho.
                // if (!EditorUtility.DisplayDialog("EZFXLayer", "Only one FXAnimationControllerSelector should exist per scene.", "OK", "Undo"))
                // {
                //     DestroyImmediate(this);
                // }
                EditorUtility.DisplayDialog("EZFXLayer", "Only one FXAnimationControllerSelector should exist per scene.", "OK");
            }
        }

        [CustomEditor(typeof(FXAnimationControllerSelector))]
        public class Editor : UnityEditor.Editor
        {
            //could add a button to select from scene, but, since this is an inspector editor, it would only be useful if going out of ones' way to lock it
            //TODO: test around an avatar not having an fx layer, and add a button to create the asset from the examples folder
            public override void OnInspectorGUI()
            {
                var target = (FXAnimationControllerSelector)base.target;
                EditorGUILayout.LabelField("The FX layer of this avatar will be modified and used across all avatars in the scene.", EditorStyles.wordWrappedLabel);
                target.targetAvatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(target.targetAvatar, typeof(VRCAvatarDescriptor), allowSceneObjects: true);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(target.FXLayerController, typeof(RuntimeAnimatorController), allowSceneObjects: true);
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}