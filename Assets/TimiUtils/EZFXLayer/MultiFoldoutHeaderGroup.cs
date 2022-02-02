#if UNITY_EDITOR
namespace TimiUtils.EZFXLayer
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using VRC.SDK3.Avatars.Components;

    public class MultiFoldoutHeaderGroup
    {
        private readonly Dictionary<object, bool> foldoutStatus = new Dictionary<object, bool>();

        public bool Begin(object key, string content, bool defaultFoldoutStatus = true)
            => foldoutStatus[key] = EditorGUILayout.BeginFoldoutHeaderGroup(
                foldoutStatus.TryGetValue(key, out var currentFoldoutStatus)
                    ? currentFoldoutStatus
                    : defaultFoldoutStatus,
                content);
        public void End() => EditorGUILayout.EndFoldoutHeaderGroup();
    }
}
#endif
