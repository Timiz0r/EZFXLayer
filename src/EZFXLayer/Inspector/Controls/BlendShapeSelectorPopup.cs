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
        private IEnumerable<GameObject> avatarGameObjects;
        private bool isReference;

        public void CreateGUI()
        {
            ScrollView scrollView = new ScrollView();
            rootVisualElement.Add(scrollView);

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
                                if (isReference)
                                {
                                    configurationOperations.AddReferenceBlendShape(smr, blendShape);
                                }
                                //comes later
                                // else
                                // {
                                //     configurationOperations.AddAnimationBlendShape(smr, blendShape);
                                // }
                            }
                            else
                            {
                                if (isReference)
                                {
                                    configurationOperations.RemoveReferenceBlendShape(smr, blendShape);
                                }
                                //comes later
                                // else
                                // {
                                //     configurationOperations.RemoveAnimationBlendShape(smr, blendShape);
                                // }
                            }
                        });
                        avatarFoldout.Add(blendShapeToggle);
                    }

                    avatarFoldout.value = avatarFoldout.Query<Toggle>().Where(t => t.value).First() != null;
                }
            }
        }

        public static void Show(
            Rect buttonBox, ConfigurationOperations configurationOperations, Scene scene, bool isReference)
        {
            BlendShapeSelectorPopup window = CreateInstance<BlendShapeSelectorPopup>();
            window.configurationOperations = configurationOperations;
            window.avatarGameObjects = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<VRCAvatarDescriptor>(includeInactive: true))
                .Select(c => c.gameObject);
            window.isReference = isReference;

            window.ShowAsDropDown(
                GUIUtility.GUIToScreenRect(buttonBox),
                window.position.size);
        }
    }
}
