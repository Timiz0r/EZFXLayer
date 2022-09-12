namespace EZFXLayer.UIElements
{
    using UnityEditor;

    public interface IRebindable
    {
        void Rebind(SerializedProperty serializedProperty);
    }
}
