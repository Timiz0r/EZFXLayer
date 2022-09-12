namespace EZFXLayer.UIElements
{
    using System;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    public class AnimationConfigurationField : BindableElement, IRebindable
    {
        //TODO: this should be able to be replaced by looking at isDefaultState
        private readonly bool canModify;
        private readonly AnimatorLayerComponentEditor editor;
        private SerializedPropertyContainer<AnimatableBlendShapeField> blendShapes;

        public AnimationConfigurationField(bool canModify, AnimatorLayerComponentEditor editor)
        {
            this.canModify = canModify;
            this.editor = editor;

            //TODO: on second thought, go with serialized fields since we dont have to hard code paths
            //prob not this, but other controls may move to a common area
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimationConfigurationField.uxml");
            visualTree.CloneTree(this);

            if (canModify)
            {
                this.Query(className: "animation-immutable").ForEach(e => e.RemoveFromClassList("animation-immutable"));
                this.Q<UnityEngine.UIElements.Button>(name: "addBlendShape").clicked += () =>
                {
                    //TODO: ofc we'll need a picker
                    //slightly circular, since we can add here directly, but this keeps it consistent with deletes
                    editor.AddBlendShape(new AnimatableBlendShape()
                    {
                        skinnedMeshRenderer = null,
                        name = $"florp{blendShapes.All.Count()}",
                        value = 0
                    });
                };
            }
        }

        public void DeleteBlendShape(AnimatableBlendShape blendShape)
        {
            blendShapes.Delete(sp => AnimatableBlendShapeField.Deserialize(sp).Matches(blendShape));
        }

        public void AddBlendShape(AnimatableBlendShape blendShape)
        {
            blendShapes.Add(sp =>
            {
                sp.FindPropertyRelative("skinnedMeshRenderer").objectReferenceValue = blendShape.skinnedMeshRenderer;
                sp.FindPropertyRelative("name").stringValue = blendShape.name;
                sp.FindPropertyRelative("value").floatValue = blendShape.value;
            });
        }

        public void Rebind(SerializedProperty serializedProperty)
        {
            this.BindProperty(serializedProperty);

            //the attribute-based way isn't working properly, so we'll keep on doing this for now
            FoldoutWithContainer foldout = this.Q<FoldoutWithContainer>();
            VisualElement foldoutContent = this.Q<VisualElement>(name: "foldoutContent");
            foldout.ConfigureSeparateContainer(foldoutContent);

            VisualElement blendShapeContainer = foldoutContent.Q<VisualElement>(name: "blendShapes");
            SerializedProperty blendShapeProperty = serializedProperty.FindPropertyRelative("blendShapes");
            blendShapes = new SerializedPropertyContainer<AnimatableBlendShapeField>(
                blendShapeContainer, blendShapeProperty, () => new AnimatableBlendShapeField(canModify, editor));

            VisualElement gameObjectContainer = foldoutContent.Q<VisualElement>(name: "gameObjects");

            //the beauty here is that, even if we have a completely different animation after rebind,
            //the refreshes will take care of everything. no need to, for instance, clear out the containers beforehand
            blendShapes.Refresh();
        }
    }
}
