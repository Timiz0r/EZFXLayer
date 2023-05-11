namespace EZUtils.EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEngine.UIElements;
    public class HelpBox : TextElement
    {
        private new class UxmlFactory : UxmlFactory<HelpBox, UxmlTraits> { }

        private new class UxmlTraits : TextElement.UxmlTraits
        {
            private readonly UxmlEnumAttributeDescription<MessageType> messageTypeAttribute =
                new UxmlEnumAttributeDescription<MessageType>()
                {
                    name = "message-type"
                };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                MessageType messageType = messageTypeAttribute.GetValueFromBag(bag, cc);

#pragma warning disable IDE0079 //vs doesnt seem to detect it
#pragma warning disable CA2000 //imguicontainer is disposable
                ve.Add(new IMGUIContainer(() => EditorGUILayout.HelpBox(((HelpBox)ve).text, messageType)));
#pragma warning restore CA2000
#pragma warning restore IDE0079
            }
        }

        public HelpBox()
        {
            generateVisualContent = null;
        }
    }
}
