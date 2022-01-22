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
                if (target.defaultAnimationSet.showBlendShapes =
                    EditorGUILayout.BeginFoldoutHeaderGroup(target.defaultAnimationSet.showBlendShapes, "Blend shapes")
                )
                {
                    //TODO: might do the skinnedmeshrenderer-based grouping here, as well
                    foreach (var blendShape in target.defaultAnimationSet.blendShapes)
                    {
                        EditorGUILayout.LabelField($"{blendShape.skinnedMeshRenderer.name}_{blendShape.name}_{blendShape.value}");
                    }

                    if (Button("Select blend shapes", out var selectBlendShapesButtonRect))
                    {
                        PopupWindow.Show(
                            selectBlendShapesButtonRect,
                            new BlendShapeSelectionPopup(
                                target.gameObject.scene,
                                target.defaultAnimationSet.blendShapes,
                                blendShapeRecords => target.UpdateBlendShapeSelection(
                                    target.defaultAnimationSet, blendShapeRecords)));
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                if (target.defaultAnimationSet.showGameObjects =
                    EditorGUILayout.BeginFoldoutHeaderGroup(target.defaultAnimationSet.showGameObjects, "GameObjects")
                )
                {
                    foreach (var gameObject in target.defaultAnimationSet.gameObjects)
                    {
                        EditorGUILayout.LabelField($"{gameObject.gameObject.name}_{gameObject.path}_{gameObject.active}");
                    }

                    var newGameObject = (GameObject)EditorGUILayout.ObjectField(null, typeof(GameObject), allowSceneObjects: true);
                    if (newGameObject != null)
                    {
                        target.defaultAnimationSet.gameObjects.Add(new AnimationSet.AnimatableGameObject()
                        {
                            gameObject = newGameObject,
                            path = GetPath(newGameObject)
                        });
                        newGameObject = null;
                    }
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Separator();


            }
        }

        private static bool Button(string text, out Rect rect)
        {
            var content = new GUIContent(text);
            rect = GUILayoutUtility.GetRect(content, GUI.skin.button);
            return GUI.Button(rect, content, GUI.skin.button);
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
