namespace EZUtils.EZFXLayer
{
    using UnityEditor;
    using UnityEngine;

    internal static class ConfigSerialization
    {
        public static AnimatableBlendShape DeserializeBlendShape(SerializedProperty serializedProperty)
            => new AnimatableBlendShape(
                skinnedMeshRenderer: (SkinnedMeshRenderer)serializedProperty.FindPropertyRelative(
                    nameof(AnimatableBlendShape.skinnedMeshRenderer)).objectReferenceValue,
                name: serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.name)).stringValue,
                value: serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.value)).floatValue,
                synchronizeValueWithReference: serializedProperty.FindPropertyRelative(
                    nameof(AnimatableBlendShape.synchronizeValueWithReference)).boolValue,
                disabled: serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.disabled)).boolValue)
            {
                key = serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.key)).stringValue,
            };

        public static void SerializeBlendShape(SerializedProperty serializedProperty, AnimatableBlendShape blendShape)
        {
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.key)).stringValue = blendShape.key;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.skinnedMeshRenderer)).objectReferenceValue =
                blendShape.skinnedMeshRenderer;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.name)).stringValue = blendShape.name;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.value)).floatValue = blendShape.value;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.synchronizeValueWithReference)).boolValue =
                blendShape.synchronizeValueWithReference;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.disabled)).boolValue =
                blendShape.disabled;
        }

        public static AnimatableGameObject DeserializeGameObject(SerializedProperty serializedProperty)
        {
            AnimatableGameObject result = new AnimatableGameObject(
                gameObject: (GameObject)serializedProperty.FindPropertyRelative(
                    nameof(AnimatableGameObject.gameObject)).objectReferenceValue,
                path: serializedProperty.FindPropertyRelative(nameof(AnimatableGameObject.path)).stringValue,
                active: serializedProperty.FindPropertyRelative(nameof(AnimatableGameObject.active)).boolValue,
                synchronizeActiveWithReference: serializedProperty.FindPropertyRelative(
                    nameof(AnimatableGameObject.synchronizeActiveWithReference)).boolValue,
                disabled: serializedProperty.FindPropertyRelative(nameof(AnimatableGameObject.disabled)).boolValue)
            {
                key = serializedProperty.FindPropertyRelative(nameof(AnimatableGameObject.key)).stringValue,
            };
            return result;
        }

        internal static void SerializeGameObject(SerializedProperty serializedProperty, AnimatableGameObject gameObject)
        {
            serializedProperty.FindPropertyRelative(nameof(AnimatableGameObject.key)).stringValue = gameObject.key;
            serializedProperty.FindPropertyRelative(nameof(AnimatableGameObject.gameObject)).objectReferenceValue =
                gameObject.gameObject;
            serializedProperty.FindPropertyRelative(nameof(AnimatableGameObject.path)).stringValue = gameObject.path;
            serializedProperty.FindPropertyRelative(nameof(AnimatableGameObject.active)).boolValue = gameObject.active;
            serializedProperty.FindPropertyRelative(nameof(AnimatableGameObject.synchronizeActiveWithReference)).boolValue =
                gameObject.synchronizeActiveWithReference;
            serializedProperty.FindPropertyRelative(nameof(AnimatableGameObject.disabled)).boolValue =
                gameObject.disabled;
        }
    }
}
