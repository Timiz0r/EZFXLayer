namespace EZFXLayer.UIElements
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    public class HelpBox : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<HelpBox, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription labelAttribute = new UxmlStringAttributeDescription()
            {
                name = "label"
            };
            private readonly UxmlEnumAttributeDescription<MessageType> messageTypeAttribute =
                new UxmlEnumAttributeDescription<MessageType>()
                {
                    name = "message-type"
                };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                string text = labelAttribute.GetValueFromBag(bag, cc);
                MessageType messageType = messageTypeAttribute.GetValueFromBag(bag, cc);

                ve.Add(new IMGUIContainer(() => EditorGUILayout.HelpBox(text, messageType)));
            }
        }
    }
}
