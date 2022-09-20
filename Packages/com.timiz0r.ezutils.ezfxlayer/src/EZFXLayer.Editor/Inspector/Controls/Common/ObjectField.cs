namespace EZUtils.EZFXLayer.UIElements
{
    using System;
    using UnityEngine.UIElements;
    using OriginalObjectField = UnityEditor.UIElements.ObjectField;
    public class ObjectField : OriginalObjectField
    {
        private new class UxmlFactory : UxmlFactory<ObjectField, UxmlTraits> { }

        private new class UxmlTraits : OriginalObjectField.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription objectTypeAttribute = new UxmlStringAttributeDescription
            {
                //future unity versions have type attribute we wont want to conflict with
                name = "objectType"
            };
            private readonly UxmlBoolAttributeDescription enabledAttribute = new UxmlBoolAttributeDescription
            {
                name = "enabled",
                defaultValue = true
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                string typeString = objectTypeAttribute.GetValueFromBag(bag, cc);
                Type type = Type.GetType(typeString);

                ((ObjectField)ve).objectType = type;

                bool isEnabled = enabledAttribute.GetValueFromBag(bag, cc);
                ve.SetEnabled(isEnabled);
            }
        }
    }
}
