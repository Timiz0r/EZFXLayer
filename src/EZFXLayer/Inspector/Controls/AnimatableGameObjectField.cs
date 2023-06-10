namespace EZUtils.EZFXLayer.UIElements
{
    using EZFXLayer;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    using static Localization;

    internal class AnimatableGameObjectField : BindableElement, ISerializedPropertyContainerItem
    {
        private readonly ConfigurationOperations configOperations;
        private readonly bool isReference;

        public AnimatableGameObject GameObject { get; private set; }

        public AnimatableGameObjectField(
            ConfigurationOperations configOperations,
            bool isReference)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/Controls/AnimatableGameObjectField.uxml");
            visualTree.CloneTree(this);
            TranslateElementTree(this);

            this.configOperations = configOperations;
            this.isReference = isReference;

            this.Q<Button>().clicked += () =>
            {
                if (isReference)
                {
                    this.configOperations.RemoveReferenceGameObject(GameObject);
                }
                //comes later
                // else
                // {
                //     this.configOperations.RemoveAnimationGameObject(GameObject);
                // }
            };

            _ = this.Q<Toggle>().RegisterValueChangedCallback(evt =>
            {
                GameObject.active = evt.newValue;

                if (isReference)
                {
                    this.configOperations.ReferenceGameObjectChanged();
                }
                else
                {
                    CheckForReferenceMatch();
                }
            });
        }

        public void CheckForReferenceMatch()
            => EnableInClassList("blendshape-matches-reference", configOperations.GameObjectMatchesReference(GameObject));

        public void Rebind(SerializedProperty serializedProperty)
        {
            GameObject = ConfigSerialization.DeserializeGameObject(serializedProperty);
            //note that under some circumstances (including redos), a rebind will trigger the toggle's
            //RegisterValueChangedCallback, so we want the GameObject it uses to be initialized first
            this.BindProperty(serializedProperty);

            if (!isReference)
            {
                CheckForReferenceMatch();
            }
        }
    }
}
