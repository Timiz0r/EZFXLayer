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

            this.Q<Button>().clicked += () => this.configurator.RemoveGameObject(GameObject);

            _ = this.Q<Toggle>().RegisterValueChangedCallback(evt => GameObject.active = evt.newValue);
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
