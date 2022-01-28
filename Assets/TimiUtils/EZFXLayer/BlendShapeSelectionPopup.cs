namespace TimiUtils.EZFXLayer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using VRC.SDK3.Avatars.Components;

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
                    foreach (var blendShape in mesh.GetBlendShapeNames())
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
