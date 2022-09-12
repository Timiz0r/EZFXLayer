namespace EZFXLayer.UIElements
{
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    public class AnimationConfigurationField : BindableElement, ISerializedPropertyContainerItem
    {
        private readonly AnimatorLayerComponentEditor editor;
        private SerializedPropertyContainer<AnimatableBlendShapeField> blendShapes;
        private string animationConfigurationKey = null;

        public AnimationConfigurationField(AnimatorLayerComponentEditor editor)
        {
            this.editor = editor;

            //TODO: on second thought, go with serialized fields since we dont have to hard code paths
            //prob not this, but other controls may move to a common area
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimationConfigurationField.uxml");
            visualTree.CloneTree(this);

            this.Q<UnityEngine.UIElements.Button>(name: "addBlendShape").clicked += () =>
            {
                //TODO: ofc we'll need a picker
                //slightly circular, since we can add here directly, but this keeps it consistent with removes
                editor.AddBlendShape(new AnimatableBlendShape()
                {
                    skinnedMeshRenderer = null,
                    name = $"florp{blendShapes.Count}",
                    value = 0
                });
            };

            this.Q<UnityEngine.UIElements.Button>(name: "removeAnimationConfiguration").clicked += () =>
            {
                editor.RemoveAnimation(animationConfigurationKey);
            };
        }

        public void RemoveBlendShape(AnimatableBlendShape blendShape)
        {
            blendShapes.Remove(sp => AnimatableBlendShapeField.Deserialize(sp).Matches(blendShape), apply: false);
        }

        public void AddBlendShape(AnimatableBlendShape blendShape)
        {
            blendShapes.Add(sp =>
            {
                AnimatableBlendShapeField.Serialize(sp, blendShape);
            }, apply: false);
        }

        public void Rebind(SerializedProperty serializedProperty)
        {
            this.BindProperty(serializedProperty);

            bool isReferenceAnimation =
                serializedProperty.FindPropertyRelative(nameof(AnimationConfiguration.isReferenceAnimation)).boolValue;
            if (isReferenceAnimation)
            {
                AddToClassList("reference-animation");
            }

            animationConfigurationKey =
                serializedProperty.FindPropertyRelative(nameof(AnimationConfiguration.key)).stringValue;

            //TODO: the attribute-based way isn't working properly, so we'll keep on doing this for now
            //also is faulty because it's coded to start at the root, and we'll have many animation configurations
            //aka all fold and unfold with the attribute
            FoldoutWithContainer foldout = this.Q<FoldoutWithContainer>();
            VisualElement foldoutContent = this.Q<VisualElement>(name: "foldoutContent");
            foldout.ConfigureSeparateContainer(foldoutContent);

            VisualElement blendShapeContainer = foldoutContent.Q<VisualElement>(name: "blendShapes");
            SerializedProperty blendShapeProperty = serializedProperty.FindPropertyRelative("blendShapes");
            blendShapes = new SerializedPropertyContainer<AnimatableBlendShapeField>(
                blendShapeContainer, blendShapeProperty, () => new AnimatableBlendShapeField(editor));

            VisualElement gameObjectContainer = foldoutContent.Q<VisualElement>(name: "gameObjects");

            //the beauty here is that, even if we have a completely different animation after rebind,
            //the refreshes will take care of everything. no need to, for instance, clear out the containers beforehand
            blendShapes.Refresh();
        }
    }
}
