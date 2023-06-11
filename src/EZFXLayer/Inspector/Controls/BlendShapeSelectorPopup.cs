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
        private IAnimatableConfigurator configurator;
        private IEnumerable<GameObject> avatarGameObjects;

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
                        bool isSelected = configurator.IsBlendShapeSelected(
                            smr, blendShape, out bool permanent, out string key);
                        Toggle blendShapeToggle = new Toggle()
                        {
                            text = blendShape,
                            value = isSelected,
                            userData = key
                        };
                        if (permanent)
                        {
                            blendShapeToggle.SetEnabled(false);
                        }

                        _ = blendShapeToggle.RegisterValueChangedCallback(evt =>
                        {
                            AnimatableBlendShape animatableBlendShape = new AnimatableBlendShape()
                            {
                                skinnedMeshRenderer = smr,
                                name = blendShape
                            };
                            //if not given as part of IsBlendShapeSelected, then we'll end up using the auto-gen'd key
                            //if a selected blendshape is deselected then selected, we also get to reuse the key, which is nice
                            //though it's worth noting we'd lose values as currently designed. this is somewhat preferable,
                            //since we have a convenient way to reset a blendshape, though the most ideal implementation
                            //is to only apply changes when the popup is closed, and add a reset button instead.
                            //pretty low-pri though.
                            animatableBlendShape.key = (string)((VisualElement)evt.target).userData
                                ?? animatableBlendShape.key;

                            if (evt.newValue)
                            {
                                configurator.AddBlendShape(animatableBlendShape);
                            }
                            else
                            {
                                configurator.RemoveBlendShape(animatableBlendShape);
                            }
                        });
                        avatarFoldout.Add(blendShapeToggle);
                    }

                    avatarFoldout.value = avatarFoldout.Query<Toggle>().Where(t => t.value).First() != null;
                }
            }
        }

        public static void Show(Rect buttonBox, IAnimatableConfigurator configurator, Scene scene)
        {
            BlendShapeSelectorPopup window = CreateInstance<BlendShapeSelectorPopup>();
            window.configurator = configurator;
            window.avatarGameObjects = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<VRCAvatarDescriptor>(includeInactive: true))
                .Select(c => c.gameObject);

            window.ShowAsDropDown(
                GUIUtility.GUIToScreenRect(buttonBox),
                window.position.size);
        }
    }
}
