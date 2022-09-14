namespace EZFXLayer
{
    using UnityEditor;
    using UnityEngine;

    internal static class ConfigSerialization
    {
        public static AnimatableBlendShape DeserializeBlendShape(SerializedProperty serializedProperty)
        {
            AnimatableBlendShape result = new AnimatableBlendShape()
            {
                key = serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.key)).stringValue,
                skinnedMeshRenderer = (SkinnedMeshRenderer)serializedProperty.FindPropertyRelative(
                    nameof(AnimatableBlendShape.skinnedMeshRenderer)).objectReferenceValue,
                name = serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.name)).stringValue,
                value = serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.value)).floatValue
            };
            return result;
        }

        public static void SerializeBlendShape(SerializedProperty serializedProperty, AnimatableBlendShape blendShape)
        {
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.key)).stringValue = blendShape.key;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.skinnedMeshRenderer)).objectReferenceValue =
                blendShape.skinnedMeshRenderer;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.name)).stringValue = blendShape.name;
            serializedProperty.FindPropertyRelative(nameof(AnimatableBlendShape.value)).floatValue = blendShape.value;
        }
    }
}
