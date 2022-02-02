#if UNITY_EDITOR
namespace TimiUtils.EZFXLayer
{
    using System;
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
        public List<AnimationSet> animationSets = new List<AnimationSet>();

        //parameter name to be layerName
        public bool manageStateMachine = true;
        public string menuPath = null;
        //useful for viewing, in some cases
        public bool hideUnchangedItemsInAnimationSets = false;

        public void Reset()
        {
            //for the initial value of the component
            //but we dont exclusively go with it because we dont disallow multiple components per GameObject
            //TODO: get more unique default names. tho, still will be uncommon to have multiple per gameobject.
            layerName = layerName ?? this.gameObject.name;
        }

        public void UpdateBlendShapeSelection(AnimationSet animationSet, IReadOnlyList<BlendShapeSelectionPopup.BlendShapeRecord> blendShapeRecords)
        {
            Undo.RecordObject(this, "Changed selection of blend shapes");
            foreach (var record in blendShapeRecords)
            {
                if (record.AlreadySelected == record.CurrentlySelected) continue;

                if (record.CurrentlySelected)
                {
                    //was considering inserting this into the same place it would be in the list of blendshapes
                    //but since that order can just change thanks to mesh changes, we'll just order it in editor
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

        public void AddAnimationSet()
        {
            var animationSet = new AnimationSet() { name = $"{gameObject.name}_{animationSets.Count}" };
            animationSet.blendShapes.AddRange(defaultAnimationSet.blendShapes.Select(bs => bs.Clone()));
            animationSet.gameObjects.AddRange(defaultAnimationSet.gameObjects.Select(go => go.Clone()));
            animationSets.Add(animationSet);
        }

        [CustomEditor(typeof(AnimatorLayer))]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                var target = (AnimatorLayer)base.target;

                target.layerName = EditorGUILayout.DelayedTextField("Name", target.layerName);
                target.menuPath = EditorGUILayout.DelayedTextField("Menu path", target.menuPath);
                target.manageStateMachine = EditorGUILayout.ToggleLeft(
                    "Manage states, conditions, and parameters", target.manageStateMachine);
                EditorGUILayout.Separator();

                if (target.defaultAnimationSet.isFoldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(
                    target.defaultAnimationSet.isFoldedOut, "Default animation set"))
                {
                    //TODO: button indenting
                    //indenting off for now because the toggle for gameobjects ends up behind the button for some reason
                    //EditorGUI.indentLevel++;
                    RenderAnimationSetEditor(target, target.defaultAnimationSet, defaultAnimationSet: null);

                    foreach (var animationSet in target.animationSets)
                    {
                        animationSet.ProcessUpdatedDefault(target.defaultAnimationSet);
                    }
                    //EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Separator();

                target.hideUnchangedItemsInAnimationSets = EditorGUILayout.ToggleLeft(
                    "Hide unchanged items", target.hideUnchangedItemsInAnimationSets);

                if (GUILayout.Button("Add new animation set"))
                {
                    target.AddAnimationSet();
                }

                AnimationSet animationSetToDelete = null;
                foreach (var animationSet in target.animationSets)
                {
                    animationSet.isFoldedOut = FoldoutWithControls(
                        animationSet.isFoldedOut,
                        content: null,
                        foldoutContents: () =>
                        {
                            //EditorGUI.indentLevel++;
                            RenderAnimationSetEditor(
                                target, animationSet, defaultAnimationSet: target.defaultAnimationSet);
                            animationSet.menuPath = EditorGUILayout.DelayedTextField(
                                "Menu path", animationSet.menuPath);
                            animationSet.animatorStateNameOverride = EditorGUILayout.DelayedTextField(
                                "State name override", animationSet.animatorStateNameOverride);
                            //EditorGUI.indentLevel--;
                        },
                        foldoutControls: () =>
                        {
                            animationSet.name = EditorGUILayout.DelayedTextField(animationSet.name);
                            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                            {
                                animationSetToDelete = animationSet;
                            }
                        }
                    );
                    EditorGUILayout.Separator();
                }
                if (animationSetToDelete != null)
                {
                    target.animationSets.Remove(animationSetToDelete);
                }
            }

            //TODO: undo doesn't seem to work, so gotta do it manually!
            //TODO: add a button for populating the base controller with a placeholder layer and states
            //  but not transitions, unless we wanna generate parameters too. not generating parameters reduces the
            //impact of stale states from a rename. do we wanna force transition generation toggle on if off?
            private void RenderAnimationSetEditor(
                AnimatorLayer animatorLayer, AnimationSet animationSet, AnimationSet defaultAnimationSet)
            {
                bool isDefaultAnimationSet = defaultAnimationSet == null;
                EditorGUILayout.LabelField("Blend shapes", EditorStyles.boldLabel);

                AnimationSet.AnimatableBlendShape blendShapeToDelete = null;
                foreach (var smrGroup in animationSet.blendShapes.GroupBy(bs => bs.skinnedMeshRenderer))
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(smrGroup.Key, typeof(SkinnedMeshRenderer), allowSceneObjects: true);
                    EditorGUI.EndDisabledGroup();

                    var meshBlendShapes = smrGroup.Key.sharedMesh.GetBlendShapeNames().ToList();
                    var blendShapeAdded = false;
                    foreach (var blendShape in smrGroup.OrderBy(bs => meshBlendShapes.FindIndex(bsn => bs.name == bsn)))
                    {
                        if (!isDefaultAnimationSet && animatorLayer.hideUnchangedItemsInAnimationSets
                            && defaultAnimationSet.HasIdenticalBlendShape(blendShape))
                        {
                            continue;
                        }

                        blendShapeAdded = true;
                        EditorGUILayout.BeginHorizontal();
                        blendShape.value = EditorGUILayout.Slider(blendShape.name, blendShape.value, 0, 100);

                        if (isDefaultAnimationSet && GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                        {
                            blendShapeToDelete = blendShape;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (!blendShapeAdded)
                    {
                        EditorGUILayout.LabelField("None");
                    }
                }
                if (blendShapeToDelete != null)
                {
                    animationSet.blendShapes.Remove(blendShapeToDelete);
                }
                if (isDefaultAnimationSet && Button("Select blend shapes", out var selectBlendShapesButtonRect))
                {
                    PopupWindow.Show(
                        selectBlendShapesButtonRect,
                        new BlendShapeSelectionPopup(
                            animatorLayer.gameObject.scene,
                            animationSet.blendShapes,
                            blendShapeRecords => animatorLayer.UpdateBlendShapeSelection(
                                animationSet, blendShapeRecords)));
                }
                EditorGUILayout.Separator();

                EditorGUILayout.LabelField("GameObjects", EditorStyles.boldLabel);
                AnimationSet.AnimatableGameObject gameObjectToDelete = null;
                var gameObjectAdded = false;
                foreach (var gameObject in animationSet.gameObjects)
                {
                    if (!isDefaultAnimationSet && animatorLayer.hideUnchangedItemsInAnimationSets
                        && defaultAnimationSet.HasIdenticalGameObject(gameObject))
                    {
                        continue;
                    }

                    gameObjectAdded = true;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(gameObject.gameObject, typeof(GameObject), allowSceneObjects: true);
                    EditorGUI.EndDisabledGroup();

                    gameObject.active = Checkbox(gameObject.active);

                    if (isDefaultAnimationSet && GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    {
                        gameObjectToDelete = gameObject;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (!isDefaultAnimationSet && !gameObjectAdded)
                {
                    EditorGUILayout.LabelField("None");
                }
                if (gameObjectToDelete != null)
                {
                    animationSet.gameObjects.Remove(gameObjectToDelete);
                }

                if (isDefaultAnimationSet)
                {
                    var newGameObject = (GameObject)EditorGUILayout.ObjectField(
                        "Add GameObject", null, typeof(GameObject), allowSceneObjects: true);
                    if (newGameObject != null)
                    {
                        animationSet.gameObjects.Add(new AnimationSet.AnimatableGameObject()
                        {
                            gameObject = newGameObject,
                            path = newGameObject.GetRelativePath()
                        });
                        newGameObject = null;
                    }
                }
                EditorGUILayout.Separator();
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

            private static bool FoldoutWithControls(bool foldout, string content, Action foldoutContents, Action foldoutControls)
            {
                //still a small gap between foldout icon and foldoutControls when no content, but not too bad
                //gap is bigger with EditorGUILayout
                var guiContent = new GUIContent(content);
                EditorGUILayout.BeginHorizontal();
                if (foldout = EditorGUI.Foldout(
                    GUILayoutUtility.GetRect(guiContent, EditorStyles.foldoutHeader, GUILayout.ExpandWidth(false)),
                    foldout,
                    guiContent
                ))
                {
                    foldoutControls();
                    EditorGUILayout.EndHorizontal();

                    foldoutContents();
                }
                else
                {
                    foldoutControls();
                    EditorGUILayout.EndHorizontal();
                }
                return foldout;
            }
        }
    }
}
#endif
