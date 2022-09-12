namespace EZFXLayer.UIElements
{
    using UnityEditor;

    public interface ISerializedPropertyContainerItem
    {
        void Rebind(SerializedProperty serializedProperty);
    }
}
