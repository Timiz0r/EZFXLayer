namespace EZFXLayer.UIElements
{
    using System;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using OriginalButton = UnityEngine.UIElements.Button;

    //as needed, should use an interface instead
    //still have this class tho
    public class ViewModelVisualElement : VisualElement
    {
        public object ViewModel { get; set; }

        public new class UxmlFactory : UxmlFactory<ViewModelVisualElement, UxmlTraits> { }

        //could use this to "bind" to a subobject
        public new class UxmlTraits : VisualElement.UxmlTraits
        {

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) => base.Init(ve, bag, cc);
        }
    }
}
