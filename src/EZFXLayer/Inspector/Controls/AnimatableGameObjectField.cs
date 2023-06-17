namespace EZUtils.EZFXLayer.UIElements
{
    using EZFXLayer;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    using static Localization;

    internal class AnimatableGameObjectField : BindableElement, ISerializedPropertyContainerItem
    {
        private readonly IAnimatableConfigurator configurator;

        public AnimatableGameObject GameObject { get; private set; }

        public AnimatableGameObjectField(IAnimatableConfigurator configurator)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/Controls/AnimatableGameObjectField.uxml");
            visualTree.CloneTree(this);
            TranslateElementTree(this);

            this.configurator = configurator;

            VisualElement fieldContainer = this.Q<VisualElement>(className: "animatable-field-container");
            Toggle disabledToggle = this.Q<Toggle>(name: "disabled");
            _ = disabledToggle.RegisterValueChangedCallback(
                evt => fieldContainer.EnableInClassList("animatable-disabled", evt.newValue));
            _ = schedule.Execute(() => fieldContainer.EnableInClassList("animatable-disabled", disabledToggle.value));

            VisualElement objectFieldContainer = this.Q<VisualElement>(name: "gameObjectContainer");
            EZUtils.EZFXLayer.UIElements.ObjectField objectField =
                objectFieldContainer.Q<EZUtils.EZFXLayer.UIElements.ObjectField>();
            objectFieldContainer.RegisterCallback<MouseDownEvent>(mouseUpEvent =>
            {
                if (!(objectField.value is UnityEngine.Object targetObject)) return;

                if (mouseUpEvent.clickCount == 1)
                {
                    EditorGUIUtility.PingObject(targetObject);
                }
                else if (mouseUpEvent.clickCount == 2)
                {
                    _ = AssetDatabase.OpenAsset(targetObject);
                }
            });

            this.Q<Button>(name: "remove").clicked += () => this.configurator.RemoveGameObject(GameObject);
            //note that this button will be hidden unless disabled
            //and whether or not disabled is able to be set is controlled by ref and anim fields
            this.Q<Button>(name: "add").clicked += () => this.configurator.AddGameObject(GameObject);

            _ = this.Q<Toggle>(name: "activeToggle").RegisterValueChangedCallback(evt => GameObject.active = evt.newValue);
        }

        public void Rebind(SerializedProperty serializedProperty)
        {
            GameObject = ConfigSerialization.DeserializeGameObject(serializedProperty);
            //note that under some circumstances (including redos), a rebind will trigger the toggle's
            //RegisterValueChangedCallback, so we want the GameObject it uses to be initialized first
            this.BindProperty(serializedProperty);
        }
    }
}
