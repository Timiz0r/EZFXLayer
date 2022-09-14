namespace EZFXLayer.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    public class AnimationConfigurationField : BindableElement, ISerializedPropertyContainerItem
    {
        private readonly AnimatorLayerComponentEditor editor;
        private readonly bool isReferenceAnimation;
        private SerializedPropertyContainer blendShapes;
        private string animationConfigurationKey = null;

        public IEnumerable<AnimatableBlendShapeField> BlendShapes => blendShapes.AllElements<AnimatableBlendShapeField>();

        public AnimationConfigurationField(AnimatorLayerComponentEditor editor, bool isReferenceAnimation)
        {
            this.editor = editor;
            this.isReferenceAnimation = isReferenceAnimation;

            //TODO: on second thought, go with serialized fields since we dont have to hard code paths
            //prob not this, but other controls may move to a common area
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimationConfigurationField.uxml");
            visualTree.CloneTree(this);

            if (isReferenceAnimation)
            {
                AddToClassList("reference-animation");
            }

            this.Q<UnityEngine.UIElements.Button>(name: "addBlendShape").clicked += () =>
            {
                //TODO: ofc we'll need a picker
                //slightly circular, since we can add here directly, but this keeps it consistent with removes
                editor.AddBlendShape(new AnimatableBlendShape()
                {
                    skinnedMeshRenderer = ((AnimatorLayerComponent)editor.target).gameObject.scene.GetRootGameObjects()[2].GetComponentInChildren<SkinnedMeshRenderer>(),
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
            => blendShapes.Add(sp => AnimatableBlendShapeField.Serialize(sp, blendShape), apply: false);

        public void Rebind(SerializedProperty serializedProperty)
        {
            string newKey =
                serializedProperty.FindPropertyRelative(nameof(AnimationConfiguration.key)).stringValue;
            if (newKey == animationConfigurationKey) return;

            this.BindProperty(serializedProperty);
            animationConfigurationKey = newKey;

            //TODO: the attribute-based way isn't working properly, so we'll keep on doing this for now
            //also is faulty because it's coded to start at the root, and we'll have many animation configurations
            //aka all fold and unfold with the attribute
            FoldoutWithContainer foldout = this.Q<FoldoutWithContainer>();
            VisualElement foldoutContent = this.Q<VisualElement>(name: "foldoutContent");
            foldout.ConfigureSeparateContainer(foldoutContent);

            VisualElement blendShapeContainer = foldoutContent.Q<VisualElement>(name: "blendShapes");
            blendShapeContainer.Clear(); //BlendShapeContainerRenderer doesn't support reuse
            SerializedProperty blendShapesProperty = serializedProperty.FindPropertyRelative("blendShapes");
            blendShapes?.StopUndoRedoHandling(); //about to make a new one
            blendShapes = new SerializedPropertyContainer(
                blendShapesProperty, new BlendShapeContainerRenderer(blendShapeContainer, isReferenceAnimation, editor));
            blendShapes.Refresh();

            VisualElement gameObjectContainer = foldoutContent.Q<VisualElement>(name: "gameObjects");
        }

        private class BlendShapeContainerRenderer : ISerializedPropertyContainerRenderer
        {
            private readonly bool isFromReferenceAnimation;
            private readonly AnimatorLayerComponentEditor editor;
            //these BlendShapeContainers will be simple ones that we'll sort later when finalizing refresh
            private readonly Dictionary<SkinnedMeshRenderer, VisualElement> blendShapeContainers
                = new Dictionary<SkinnedMeshRenderer, VisualElement>();
            private Dictionary<AnimatableBlendShapeField, bool> refreshElementTracker;

            public BlendShapeContainerRenderer(
                VisualElement rootContainer, bool isFromReferenceAnimation, AnimatorLayerComponentEditor editor)
            {
                RootContainer = rootContainer;
                this.isFromReferenceAnimation = isFromReferenceAnimation;
                this.editor = editor;
            }

            public VisualElement RootContainer { get; }

            public void ProcessRefresh(SerializedProperty item, int index)
            {
                AnimatableBlendShape blendShape = AnimatableBlendShapeField.Deserialize(item);
                VisualElement blendShapeContainer = GetBlendShapeContainer(blendShape);

                if (index == 0)
                {
                    refreshElementTracker = RootContainer.Query<AnimatableBlendShapeField>()
                        .ToList()
                        .ToDictionary(e => e, _ => false);
                }

                AnimatableBlendShapeField matchingElement = blendShapeContainer
                    .Query<AnimatableBlendShapeField>()
                    .Where(e => e.BlendShape.Matches(blendShape))
                    .First();

                if (matchingElement == null)
                {
                    AnimatableBlendShapeField newElement = new AnimatableBlendShapeField(item, editor, isFromReferenceAnimation);
                    blendShapeContainer.Add(newElement);
                }
                else
                {
                    refreshElementTracker[matchingElement] = true;
                    //and no need to rebind what is already correctly bound
                }
            }

            public void FinalizeRefresh(SerializedProperty array)
            {
                IEnumerable<AnimatableBlendShapeField> unusedElements =
                    refreshElementTracker.Where(kvp => !kvp.Value).Select(kvp => kvp.Key);
                foreach (AnimatableBlendShapeField element in unusedElements)
                {
                    element.RemoveFromHierarchy();
                }

                foreach (VisualElement container in blendShapeContainers.Select(kvp => kvp.Value))
                {
                    container.Sort((lhs, rhs) => AnimatableBlendShapeField.Compare(
                        (AnimatableBlendShapeField)lhs,
                        (AnimatableBlendShapeField)rhs));
                }
            }

            private VisualElement GetBlendShapeContainer(AnimatableBlendShape blendShape)
            {
                if (blendShapeContainers.TryGetValue(blendShape.skinnedMeshRenderer, out VisualElement existingGroup))
                {
                    return existingGroup;
                }

                //TODO: prob make a new uxml control
                VisualElement blendShapeGroup = new VisualElement();
                blendShapeGroup.AddToClassList("blendshape-smr-group");

                ObjectField objectField = new ObjectField()
                {
                    objectType = typeof(SkinnedMeshRenderer),
                    value = blendShape.skinnedMeshRenderer,
                };
                objectField.SetEnabled(false);
                blendShapeGroup.Add(objectField);

                VisualElement blendShapeContainer = new VisualElement();
                blendShapeContainer.AddToClassList("blendshape-smr-container");
                blendShapeGroup.Add(blendShapeContainer);

                RootContainer.Add(blendShapeGroup);
                blendShapeContainers.Add(blendShape.skinnedMeshRenderer, blendShapeContainer);

                return blendShapeContainer;
            }
        }
    }
}
