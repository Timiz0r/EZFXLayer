namespace EZUtils.EZFXLayer.UIElements
{
    using System;
    using System.Linq;
    using EZFXLayer;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    using static Localization;

    //TODO: consider turning this into a toolbar control, since we could save space that way
    internal class LayerCreationButtons : VisualElement
    {
        private LayerCreationButtonTarget buttonTarget;
        private GameObject targetGameObject;

        private new class UxmlFactory : UxmlFactory<LayerCreationButtons, UxmlTraits> { }

        private new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlEnumAttributeDescription<LayerCreationButtonTarget> targetAttribute =
                new UxmlEnumAttributeDescription<LayerCreationButtonTarget>
                {
                    name = "target",
                    use = UxmlAttributeDescription.Use.Required
                };
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                LayerCreationButtonTarget target = targetAttribute.GetValueFromBag(bag, cc);
                ((LayerCreationButtons)ve).buttonTarget = target;

                switch (target)
                {
                    case LayerCreationButtonTarget.Children:
                        ve.AddToClassList("layer-creation-target-children");
                        break;
                    case LayerCreationButtonTarget.Sibling:
                        ve.AddToClassList("layer-creation-target-sibling");
                        break;
                    default:
                        break;
                }
            }
        }

        public LayerCreationButtons()
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/Controls/LayerCreationButtons.uxml");
            visualTree.CloneTree(this);

            VisualElement layerCreationSection = this.Q<VisualElement>(className: "layer-creation-container");
            TextField layerNameField = layerCreationSection.Q<TextField>();

            Button topButton = layerCreationSection.Q<Button>(name: "top");
            topButton.clicked += () =>
            {
                GameObject newObject = AddLayer(layerNameField.value);
                newObject.transform.SetAsFirstSibling();
                layerNameField.value = string.Empty;
            };

            Button bottomButton = layerCreationSection.Q<Button>(name: "bottom");
            bottomButton.clicked += () =>
            {
                _ = AddLayer(layerNameField.value);
                layerNameField.value = string.Empty;
            };

            Button aboveButton = layerCreationSection.Q<Button>(name: "above");
            aboveButton.clicked += () =>
            {
                GameObject newObject = AddLayer(layerNameField.value);
                layerNameField.value = string.Empty;
                int index = targetGameObject.transform.GetSiblingIndex();
                newObject.transform.SetSiblingIndex(index);
            };

            Button belowButton = layerCreationSection.Q<Button>(name: "below");
            belowButton.clicked += () =>
            {
                GameObject newObject = AddLayer(layerNameField.value);
                layerNameField.value = string.Empty;
                int index = targetGameObject.transform.GetSiblingIndex();
                newObject.transform.SetSiblingIndex(index + 1);
            };

            UIValidator buttonValidation = new UIValidator();
            buttonValidation.AddValueValidation(layerNameField, passCondition: v => !string.IsNullOrWhiteSpace(v));
            buttonValidation.DisableIfInvalid(topButton);
            buttonValidation.DisableIfInvalid(bottomButton);
            buttonValidation.DisableIfInvalid(aboveButton);
            buttonValidation.DisableIfInvalid(belowButton);
        }

        public void SetTarget(GameObject target) => this.targetGameObject = target;

        private GameObject AddLayer(string name)
        {
            Transform parent = GetParent();
            string newObjectName = GameObjectUtility.GetUniqueNameForSibling(parent, name);
            GameObject newObject = new GameObject(newObjectName);
            Undo.RegisterCreatedObjectUndo(newObject, T($"Add layer '{newObjectName}'"));
            newObject.transform.SetParent(parent);

            _ = newObject.AddComponent<AnimatorLayerComponent>();

            return newObject;
        }

        private Transform GetParent()
        {
            if (targetGameObject == null) throw new InvalidOperationException(
                T("Target was not set for layer creation buttons."));

            switch (buttonTarget)
            {
                case LayerCreationButtonTarget.Children:
                    return targetGameObject.transform;
                case LayerCreationButtonTarget.Sibling:
                    return targetGameObject.transform.parent;
                default:
                    throw new InvalidOperationException("Unknown LayerCreationButtonTarget.");
            }
        }
    }

    public enum LayerCreationButtonTarget
    {
        Children,
        Sibling
    }
}
