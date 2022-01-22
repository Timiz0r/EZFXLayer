namespace TimiUtils.EZFXLayer
{
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    [AddComponentMenu("EZFXLayer/EZFXLayer Animator Layer")]
    public class EZFXLayerAnimatorLayer : MonoBehaviour
    {
        public AnimationSet defaultAnimationSet = new AnimationSet();

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

        [CustomEditor(typeof(EZFXLayerAnimatorLayer))]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                var target = (EZFXLayerAnimatorLayer)base.target;

                EditorGUILayout.LabelField("Default animation set");
                RenderAnimationSetEditor(target, target.defaultAnimationSet);
                EditorGUILayout.Separator();
            }

            private void RenderAnimationSetEditor(EZFXLayerAnimatorLayer animatorLayer, AnimationSet animationSet)
            {
                if (animationSet.showBlendShapes =
                    EditorGUILayout.BeginFoldoutHeaderGroup(animationSet.showBlendShapes, "Blend shapes")
                )
                {
                    //TODO: might do the skinnedmeshrenderer-based grouping here, as well
                    foreach (var blendShape in animationSet.blendShapes)
                    {
                        EditorGUILayout.LabelField(
                            $"{blendShape.skinnedMeshRenderer.name}_{blendShape.name}_{blendShape.value}");
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
