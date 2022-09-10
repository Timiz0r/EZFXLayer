namespace EZFXLayer.UIElements
{
    using System;
    using System.Reflection;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using OriginalButton = UnityEngine.UIElements.Button;
    public class Button : OriginalButton
    {
        public new class UxmlFactory : UxmlFactory<Button, UxmlTraits> { }

        public new class UxmlTraits : OriginalButton.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription onClickAttribute = new UxmlStringAttributeDescription
            {
                name = "onClick"
            };

            //TODO: exceptions don't appear to be logged, so we should do some logging ourselves
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                string methodName = onClickAttribute.GetValueFromBag(bag, cc);
                if (string.IsNullOrEmpty(methodName)) return;

                VisualElement current = ve;
                do
                {
                    if (current is ViewModelVisualElement vmElement && vmElement.ViewModel is object viewModel)
                    {
                        Debug.LogWarning("wat");
                        Type vmType = viewModel.GetType();
                        MethodInfo method = vmType.GetMethod(methodName);
                        if (method == null) throw new ArgumentOutOfRangeException(
                            onClickAttribute.name,
                            $"Method '{methodName}' not found in '{vmType}' while attempting to bind to it.");

                        Debug.LogWarning("wat2");
                        Button button = (Button)ve;
                        button.clicked += () => method.Invoke(viewModel, Array.Empty<object>());
                        return;
                    }
                }
                while ((current = ve.parent) != null);

                throw new ArgumentOutOfRangeException(
                    onClickAttribute.name,
                    $"View model not found while attempting to bind to '{methodName}'.");
            }
        }
    }
}
