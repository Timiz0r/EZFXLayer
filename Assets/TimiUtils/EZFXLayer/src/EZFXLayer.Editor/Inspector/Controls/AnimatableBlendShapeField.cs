namespace EZFXLayer.UIElements
{
    using System;
    using EZFXLayer;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;
    public class AnimatableBlendShapeField : BindableElement
    {
        private Action deleteAction;

        public AnimatableBlendShapeField(bool canDelete)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimatableBlendShapeField.uxml");
            visualTree.CloneTree(this);

            if (canDelete)
            {
                RemoveFromClassList("shapekey-deletion-disabled");
                this.Q<UnityEngine.UIElements.Button>().clicked += () => this?.deleteAction();
            }
        }

        public void Reconfigure(Action deleteAction)
        {
            this.deleteAction = deleteAction;
        }
    }
}
