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
        public List<AnimationSet> animations = new List<AnimationSet>();

        //parameter name to be layerName
        public bool manageStateMachine = true;
        public string menuPath = null;
        public bool generateSubmenuForMultipleAnimationSets = true;

        public void Reset()
        {
            //for the initial value of the component
            //but we dont exclusively go with it because we dont disallow multiple components per GameObject
            //TODO: get more unique default names. tho, still will be uncommon to have multiple per gameobject.
            layerName = layerName ?? this.gameObject.name;
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

        public void AddAnimationSet()
        {
            var animation = new AnimationSet() { name = $"{gameObject.name}_{animations.Count}" };
            animation.blendShapes.AddRange(defaultAnimationSet.blendShapes.Select(bs => bs.Clone()));
            animation.gameObjects.AddRange(defaultAnimationSet.gameObjects.Select(go => go.Clone()));
            animations.Add(animation);
        }

        [CustomEditor(typeof(AnimatorLayer))]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                var target = (AnimatorLayer)base.target;

                target.layerName = EditorGUILayout.DelayedTextField("Name", target.layerName);
                target.menuPath = EditorGUILayout.DelayedTextField("Menu path", target.menuPath);
                //TODO: rename AnimationSet to Animation or something
                target.generateSubmenuForMultipleAnimationSets = EditorGUILayout.ToggleLeft(
                    "Generate submenu if multiple animations", target.generateSubmenuForMultipleAnimationSets);
                target.manageStateMachine = EditorGUILayout.ToggleLeft(
                    "Manage states, conditions, and parameters", target.manageStateMachine);
                EditorGUILayout.Separator();

                if (target.defaultAnimationSet.isFoldedOut = EditorGUILayout.BeginFoldoutHeaderGroup(
                    target.defaultAnimationSet.isFoldedOut, "Default animation"))
                {
                    //TODO: button indenting
                    EditorGUI.indentLevel++;
                    RenderAnimationSetEditor(target, target.defaultAnimationSet, isDefaultAnimation: true);

                    foreach (var animation in target.animations)
                    {
                        animation.ProcessUpdatedDefault(target.defaultAnimationSet);
                    }
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Separator();

                if (GUILayout.Button("Add new animation"))
                {
                    target.AddAnimationSet();
                }

                AnimationSet animationToDelete = null;
                foreach (var animation in target.animations)
                {
                    animation.isFoldedOut = FoldoutWithControls(
                        animation.isFoldedOut,
                        content: null,
                        foldoutContents: () =>
                        {
                            EditorGUI.indentLevel++;
                            RenderAnimationSetEditor(target, animation, isDefaultAnimation: false);
                            EditorGUI.indentLevel--;
                        },
                        foldoutControls: () =>
                        {
                            animation.name = EditorGUILayout.DelayedTextField(animation.name);
                            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                            {
                                animationToDelete = animation;
                            }
                        }
                    );
                    EditorGUILayout.Separator();
                }
                if (animationToDelete != null)
                {
                    target.animations.Remove(animationToDelete);
                }
            }

            //TODO: undo doesn't seem to work, so gotta do it manually!
            //TODO: add a button for populating the base controller with a placeholder layer and states
            //  but not transitions, unless we wanna generate parameters too. not generating parameters reduces the
            //impact of stale states from a rename. do we wanna force transition generation toggle on if off?
            private void RenderAnimationSetEditor(
                AnimatorLayer animatorLayer, AnimationSet animationSet, bool isDefaultAnimation)
            {
                EditorGUILayout.LabelField("Blend shapes", EditorStyles.boldLabel);
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

                        if (isDefaultAnimation && GUILayout.Button("X", GUILayout.ExpandWidth(false)))
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
                if (isDefaultAnimation && Button("Select blend shapes", out var selectBlendShapesButtonRect))
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
                foreach (var gameObject in animationSet.gameObjects)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(gameObject.gameObject, typeof(GameObject), allowSceneObjects: true);
                    EditorGUI.EndDisabledGroup();

                    gameObject.active = Checkbox(gameObject.active);

                    if (isDefaultAnimation && GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                    {
                        gameObjectToDelete = gameObject;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (gameObjectToDelete != null)
                {
                    animationSet.gameObjects.Remove(gameObjectToDelete);
                }

                if (isDefaultAnimation)
                {
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
