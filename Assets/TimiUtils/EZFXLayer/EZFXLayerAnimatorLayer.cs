namespace TimiUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using VRC.SDK3.Avatars.Components;

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
    }

    //TODO: do testing for when certain game objects from the scene are deleted, etc.
    //want to be able to maintain settings even when this happens.
    //for BlendShape, hopefully storing the skinnedmeshrenderer of an asset will work
    //for GameObject, it's hard. perhaps we store the name, allow the user to swap to a new gameobject, and we try to recover if we find matching gameobjects.
    //  or maybe, if we find all matches, we auto-recover?
    [Serializable]
    public class AnimationSet
    {
        public bool showBlendShapes = true;
        public List<AnimatableBlendShape> blendShapes = new List<AnimatableBlendShape>();

        public bool showGameObjects = true;
        public List<AnimatableGameObject> gameObjects = new List<AnimatableGameObject>();

        [Serializable]
        public class AnimatableBlendShape
        {
            public SkinnedMeshRenderer skinnedMeshRenderer;
            public string name;
            public float value;
        }

        [Serializable]
        public class AnimatableGameObject
        {
            public GameObject gameObject;
            public string path;
            public bool active;
        }
    }

    public class BlendShapeSelectionPopup : PopupWindowContent
    {
        private readonly MultiFoldoutHeaderGroup foldout = new MultiFoldoutHeaderGroup();
        private readonly List<BlendShapeRecord> blendShapes = new List<BlendShapeRecord>();
        private readonly Action<List<BlendShapeRecord>> callback;
        private Vector2 scrollPosition = Vector2.zero;
        private readonly Vector2 windowSize;

        public BlendShapeSelectionPopup(
            Scene scene,
            IReadOnlyList<AnimationSet.AnimatableBlendShape> alreadySelectedBlendShapes,
            Action<List<BlendShapeRecord>> callback)
        {
            //including inactive in case the parent gameobject is just temp deactivated
            var avatarGameObjects = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<VRCAvatarDescriptor>(includeInactive: true))
                .Select(c => c.gameObject);
            foreach (var avatar in avatarGameObjects)
            {
                foreach (var smr in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true))
                {
                    var mesh = smr.sharedMesh;
                    foreach (var blendShape in Enumerable.Range(0, mesh.blendShapeCount).Select(i => mesh.GetBlendShapeName(i)))
                    {
                        bool alreadySelected = alreadySelectedBlendShapes.Any(
                            bs => bs.skinnedMeshRenderer == smr && bs.name == blendShape);
                        blendShapes.Add(new BlendShapeRecord(avatar, smr, blendShape, alreadySelected));
                    }
                }
            }

            this.callback = callback;
            this.windowSize = new Vector2(
                Math.Max(EditorGUIUtility.currentViewWidth - 100, 200),
                500);
        }

        //public override Vector2 GetWindowSize() =>
        public override Vector2 GetWindowSize() => windowSize;

        public override void OnGUI(Rect rect)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var avatarGroup in blendShapes.GroupBy(r => r.Avatar))
            {
                var unfoldByDefault = avatarGroup.Any(r => avatarGroup.Key == r.Avatar && r.AlreadySelected);
                if (foldout.Begin(avatarGroup.Key, avatarGroup.Key.name, defaultFoldoutStatus: unfoldByDefault))
                {
                    foreach (var smrGroup in avatarGroup.GroupBy(r => r.SkinnedMeshRenderer))
                    {
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.ObjectField(smrGroup.Key, typeof(SkinnedMeshRenderer), allowSceneObjects: false);
                        EditorGUI.EndDisabledGroup();

                        foreach (var blendShape in smrGroup)
                        {
                            blendShape.CurrentlySelected =
                                EditorGUILayout.Toggle($"  {blendShape.Name}", blendShape.CurrentlySelected);
                        }
                    }
                }
                foldout.End();
            }
            EditorGUILayout.EndScrollView();
        }

        public override void OnClose() => callback(blendShapes);

        public class BlendShapeRecord
        {
            public readonly GameObject Avatar;
            public readonly SkinnedMeshRenderer SkinnedMeshRenderer;
            public readonly string Name;
            public readonly bool AlreadySelected;
            public bool CurrentlySelected;

            public BlendShapeRecord(GameObject avatar, SkinnedMeshRenderer skinnedMeshRenderer, string name, bool alreadySelected)
            {
                Avatar = avatar;
                SkinnedMeshRenderer = skinnedMeshRenderer;
                Name = name;
                AlreadySelected = CurrentlySelected = alreadySelected;
            }
        }
    }
// public class EditorWindowWithPopup : EditorWindow
// {
//     // Add menu item
//     [MenuItem("Example/Popup Example")]
//     static void Init()
//     {
//         EditorWindow window = EditorWindow.CreateInstance<EditorWindowWithPopup>();
//         window.Show();
//     }

//     Rect buttonRect;
//     void OnGUI()
//     {
//         {
//             GUILayout.Label("Editor window with Popup example", EditorStyles.boldLabel);
//             if (GUILayout.Button("Popup Options", GUILayout.Width(200)))
//             {
//                 PopupWindow.Show(buttonRect, new PopupExample());
//             }
//             if (Event.current.type == EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
//         }
//     }
// }
}
