namespace EZUtils.EZFXLayer.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UIElements;
    using VRC.SDK3.Avatars.Components;

    internal class BlendShapeSelectorPopup : EditorWindow
    {
        private ConfigurationOperations configurationOperations;
        private Scene scene;

        public void CreateGUI()
        {
            ScrollView scrollView = new ScrollView();
            rootVisualElement.Add(scrollView);

            IEnumerable<GameObject> avatarGameObjects = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<VRCAvatarDescriptor>(includeInactive: true))
                .Select(c => c.gameObject);
            foreach (GameObject avatar in avatarGameObjects)
            {
                Foldout avatarFoldout = new Foldout() { text = avatar.name, value = false };
                scrollView.Add(avatarFoldout);

                foreach (SkinnedMeshRenderer smr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true))
                {
                    string[] blendShapes = smr.GetBlendShapeNames().ToArray();
                    if (blendShapes.Length == 0) continue;

                    ObjectField meshObjectField = new ObjectField()
                    {
                        objectType = typeof(SkinnedMeshRenderer),
                        value = smr
                    };
                    meshObjectField.SetEnabled(false);
                    avatarFoldout.Add(meshObjectField);

                    foreach (string blendShape in blendShapes)
                    {
                        Toggle blendShapeToggle = new Toggle()
                        {
                            text = blendShape,
                            value = configurationOperations.HasBlendShape(smr, blendShape)
                        };
                        _ = blendShapeToggle.RegisterValueChangedCallback(evt =>
                        {
                            if (evt.newValue)
                            {
                                configurationOperations.AddBlendShape(smr, blendShape);
                            }
                            else
                            {
                                configurationOperations.RemoveBlendShape(smr, blendShape);
                            }

                            //the element hasn't been updated yet, so we'll check for 1, instead of 0
                            //actually, kinda shitty for it to close like that, so we'll avoid it
                            // avatarFoldout.value = avatarFoldout.Query<Toggle>().Where(t => t.value).AtIndex(1) != null;
                        });
                        avatarFoldout.Add(blendShapeToggle);
                    }

                    avatarFoldout.value = avatarFoldout.Query<Toggle>().Where(t => t.value).First() != null;
                }
            }
        }

        public static void Show(Rect buttonBox, ConfigurationOperations configurationOperations, Scene scene)
        {
            BlendShapeSelectorPopup window = CreateInstance<BlendShapeSelectorPopup>();
            window.configurationOperations = configurationOperations;
            window.scene = scene;

            // window.position = new Rect(popupPosition.x, popupPosition.y, window.position.width, window.position.height);
            window.ShowAsDropDown(
                GUIUtility.GUIToScreenRect(buttonBox),
                window.position.size);
        }
    }
}
