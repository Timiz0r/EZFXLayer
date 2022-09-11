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
        private string onClickMethod = string.Empty;

        public Button() : base()
        {
            clicked += HandleOnClickBinding;
        }

        private void HandleOnClickBinding()
        {
            if (string.IsNullOrEmpty(onClickMethod)) return;

            object viewModel = GetViewModel();
            if (viewModel == null) throw new ArgumentOutOfRangeException(
                "onClick",
                $"View model not found while attempting to bind to '{onClickMethod}'.");

            Type vmType = viewModel.GetType();
            MethodInfo method = vmType.GetMethod(onClickMethod);
            if (method == null) throw new ArgumentOutOfRangeException(
                "onClick",
                $"Method '{onClickMethod}' not found in '{vmType}' while attempting to bind to it.");

            //ofc would fail if it accepted arguments
            _ = method.Invoke(viewModel, Array.Empty<object>());
        }

        private object GetViewModel()
        {
            VisualElement current = this;
            do
            {
                if (current is ViewModelVisualElement vmElement && vmElement.ViewModel is object viewModel)
                {
                    return viewModel;
                }
            }
            while ((current = current.parent) != null);
            return null;
        }

        public new class UxmlFactory : UxmlFactory<Button, UxmlTraits> { }

        public new class UxmlTraits : OriginalButton.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription onClickAttribute = new UxmlStringAttributeDescription
            {
                name = "onClick"
            };
            private readonly UxmlBoolAttributeDescription enabledAttribute = new UxmlBoolAttributeDescription
            {
                name = "enabled"
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Button button = (Button)ve;
                button.onClickMethod = onClickAttribute.GetValueFromBag(bag, cc);

                bool isEnabled = enabledAttribute.GetValueFromBag(bag, cc);
                button.SetEnabled(isEnabled);
            }
        }
    }
}
