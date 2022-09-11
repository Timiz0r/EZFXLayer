namespace EZFXLayer.UIElements
{
    using System;
    using EZFXLayer;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;
    public class AnimatableBlendShapeField : BindableElement
    {
        public AnimatableBlendShapeField(bool canDelete, SerializedProperty blendShape)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimatableBlendShapeField.uxml");
            visualTree.CloneTree(this);

            this.BindProperty(blendShape);

            if (canDelete)
            {
                RemoveFromClassList("shapekey-deletion-disabled");
                this.Q<UnityEngine.UIElements.Button>().clicked += () =>
                {
                    _ = blendShape.DeleteCommand();
                    _ = blendShape.serializedObject.ApplyModifiedProperties();
                    this.RemoveFromHierarchy();
                    this.SendEvent(new DeleteEvent());
                };
            }
        }

        public class DeleteEvent : CommandEventBase<DeleteEvent>
        {
        }
    }
}
