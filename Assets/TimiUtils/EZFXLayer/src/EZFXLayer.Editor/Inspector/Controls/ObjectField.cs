namespace EZFXLayer.UIElements
{
    using System;
    using UnityEngine.UIElements;
    using OriginalObjectField = UnityEditor.UIElements.ObjectField;
    public class ObjectField : OriginalObjectField
    {
        public new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits> { }

        public new class UxmlTraits : OriginalObjectField.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription objectTypeAttribute = new UxmlStringAttributeDescription
            {
                //future unity versions have type attribute we wont want to conflict with
                name = "objectType"
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                string typeString = objectTypeAttribute.GetValueFromBag(bag, cc);
                Type type = Type.GetType(typeString);

                ((ObjectField)ve).objectType = type;
            }
        }
    }
}
