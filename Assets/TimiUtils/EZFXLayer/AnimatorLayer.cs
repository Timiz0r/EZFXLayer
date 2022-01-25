namespace TimiUtils.EZFXLayer
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    [AddComponentMenu("EZFXLayer/EZFXLayer Animator Layer")]
    public class AnimatorLayer : MonoBehaviour
    {
        public string layerName;
        public AnimationSet defaultAnimationSet = new AnimationSet() { name = "Default" };
        public List<AnimationSet> animations = new List<AnimationSet>();

        //parameter name to be layerName
        public bool manageStateMachine = true;
        public string menuPath = null;
        public bool generateSubmenuForMultipleAnimationSets = true;

        public AnimatorLayer()
        {
            //for the initial value of the component
            //but we dont exclusively go with it because we dont disallow multiple components per GameObject
            layerName = this.gameObject.name;
        }

        //TODO: in order to maintain the strict ordering, reproduce a new list each update, instead of add and remove
        public void UpdateBlendShapeSelection(AnimationSet animationSet, IReadOnlyList<BlendShapeSelectionPopup.BlendShapeRecord> blendShapeRecords)
        {
            Undo.RecordObject(this, "Changed selection of blend shapes");
            foreach (var record in blendShapeRecords)
            {
                if (record.AlreadySelected == record.CurrentlySelected) continue;

                if (record.CurrentlySelected)
                {
                    animationSet.blendShapes.Add(new AnimationSet.AnimatableBlendShape()
                    {
                        skinnedMeshRenderer = record.SkinnedMeshRenderer,
                        name = record.Name
                    });
                }
                else
                {
                    animationSet.blendShapes.RemoveAll(
                        bs => bs.skinnedMeshRenderer == record.SkinnedMeshRenderer && bs.name == record.Name);
                }
            }
        }

        //TODO: will prob also end up adding to list
        public AnimationSet CreateAnimationSet()
            => new AnimationSet() { name = $"{gameObject.name}_{animations.Count}" };

        [CustomEditor(typeof(AnimatorLayer))]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                var target = (AnimatorLayer)base.target;

                EditorGUILayout.LabelField("Default animation set");
                RenderAnimationSetEditor(target, target.defaultAnimationSet);
                EditorGUILayout.Separator();
            }

            //TODO: undo doesn't seem to work, so gotta do it manually!
            //TODO: add a button for populating the base controller with a placeholder layer and states
            //  but not transitions, unless we wanna generate parameters too. not generating parameters reduces the
            //impact of stale states from a rename. do we wanna force transition generation toggle on if off?
            private void RenderAnimationSetEditor(AnimatorLayer animatorLayer, AnimationSet animationSet)
            {
                if (animationSet.showBlendShapes =
                    EditorGUILayout.BeginFoldoutHeaderGroup(animationSet.showBlendShapes, "Blend shapes")
                )
                {
                    //TODO: might do the skinnedmeshrenderer-based grouping here, as well
                    //and might change the modeling based around that
                    AnimationSet.AnimatableBlendShape blendShapeToDelete = null;
                    foreach (var smrGroup in animationSet.blendShapes.GroupBy(bs => bs.skinnedMeshRenderer))
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(smrGroup.Key, typeof(SkinnedMeshRenderer), allowSceneObjects: true);
                        EditorGUI.EndDisabledGroup();

                        foreach (var blendShape in smrGroup)
                        {
                            EditorGUILayout.BeginHorizontal();
                            blendShape.value = EditorGUILayout.Slider(blendShape.name, blendShape.value, 0, 100);

                            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                            {
                                blendShapeToDelete = blendShape;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    if (blendShapeToDelete != null)
                    {
                        animationSet.blendShapes.Remove(blendShapeToDelete);
                    }

                    if (Button("Select blend shapes", out var selectBlendShapesButtonRect))
                    {
                        PopupWindow.Show(
                            selectBlendShapesButtonRect,
                            new BlendShapeSelectionPopup(
                                animatorLayer.gameObject.scene,
                                animationSet.blendShapes,
                                blendShapeRecords => animatorLayer.UpdateBlendShapeSelection(
                                    animationSet, blendShapeRecords)));
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                if (animationSet.showGameObjects =
                    EditorGUILayout.BeginFoldoutHeaderGroup(animationSet.showGameObjects, "GameObjects")
                )
                {
                    AnimationSet.AnimatableGameObject gameObjectToDelete = null;
                    foreach (var gameObject in animationSet.gameObjects)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(gameObject.gameObject, typeof(GameObject), allowSceneObjects: true);
                        EditorGUI.EndDisabledGroup();

                        gameObject.active = Checkbox(gameObject.active);

                        if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                        {
                            gameObjectToDelete = gameObject;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (gameObjectToDelete != null)
                    {
                        animationSet.gameObjects.Remove(gameObjectToDelete);
                    }

                    var newGameObject = (GameObject)EditorGUILayout.ObjectField(
                        "Add GameObject", null, typeof(GameObject), allowSceneObjects: true);
                    if (newGameObject != null)
                    {
                        animationSet.gameObjects.Add(new AnimationSet.AnimatableGameObject()
                        {
                            gameObject = newGameObject,
                            path = GetPath(newGameObject)
                        });
                        newGameObject = null;
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            private static bool Button(string text, out Rect rect)
            {
                var content = new GUIContent(text);
                rect = GUILayoutUtility.GetRect(content, GUI.skin.button);
                return GUI.Button(rect, content, GUI.skin.button);
            }

            //cant get EditorGUILayout to not take a bunch of space for the toggle
            private static bool Checkbox(bool value)
                => EditorGUI.Toggle(
                    GUILayoutUtility.GetRect(
                        GUIContent.none, GUI.skin.toggle, GUILayout.ExpandWidth(false)),
                    value);

            private static string GetPath(GameObject gameObject)
            {
                List<string> names = new List<string>();
                while (gameObject != null && gameObject.GetComponent<VRCAvatarDescriptor>() == null)
                {
                    names.Add(gameObject.name);
                    gameObject = gameObject.transform.parent.gameObject;
                }
                names.Reverse();
                var result = string.Join("/", names);
                return result;
            }
        }
    }
}
