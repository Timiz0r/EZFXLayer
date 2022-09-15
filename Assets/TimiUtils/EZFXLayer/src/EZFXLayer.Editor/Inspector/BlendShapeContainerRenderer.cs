namespace EZFXLayer.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal class BlendShapeContainerRenderer : ISerializedPropertyContainerRenderer
    {
        private readonly bool isFromReferenceAnimation;
        private readonly ConfigurationOperations configOperations;
        private readonly VisualTreeAsset groupVisualTree;

        //these BlendShapeContainers will be simple ones that we'll sort later when finalizing refresh
        private readonly Dictionary<SkinnedMeshRenderer, VisualElement> blendShapeContainers
            = new Dictionary<SkinnedMeshRenderer, VisualElement>();

        private Dictionary<AnimatableBlendShapeField, bool> refreshElementTracker;

        public BlendShapeContainerRenderer(
            VisualElement rootContainer, bool isFromReferenceAnimation, ConfigurationOperations configOperations)
        {
            RootContainer = rootContainer;
            this.isFromReferenceAnimation = isFromReferenceAnimation;
            this.configOperations = configOperations;

            groupVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/BlendShapeContainer.uxml");
        }

        public VisualElement RootContainer { get; }

        public void ProcessRefresh(SerializedProperty item, int index)
        {
            AnimatableBlendShape blendShape = ConfigSerialization.DeserializeBlendShape(item);
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
                AnimatableBlendShapeField newElement =
                    new AnimatableBlendShapeField(configOperations, item, isFromReferenceAnimation);
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
            if (blendShapeContainers.TryGetValue(blendShape.skinnedMeshRenderer, out VisualElement existingContainer))
            {
                return existingContainer;
            }

            VisualElement blendShapeGroup = groupVisualTree.CloneTree();
            blendShapeGroup.Q<EZFXLayer.UIElements.ObjectField>().value = blendShape.skinnedMeshRenderer;
            VisualElement blendShapeContainer = blendShapeGroup.Q<VisualElement>(className: "blendshape-smr-container");
            blendShapeContainers.Add(blendShape.skinnedMeshRenderer, blendShapeContainer);
            RootContainer.Add(blendShapeGroup);

            return blendShapeContainer;
        }
    }
}
