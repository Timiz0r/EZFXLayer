namespace EZUtils.EZFXLayer.UIElements
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    //TODO: each renderer could probably actually be its own custom control, which would be a smoother design
    //not that the current design is bad, so not a priority
    internal class BlendShapeContainerRenderer : ISerializedPropertyContainerRenderer
    {
        private readonly bool isFromReferenceAnimation;
        private readonly ConfigurationOperations configOperations;
        private readonly VisualTreeAsset groupVisualTree;

        //these BlendShapeContainers will be simple ones that we'll sort later when finalizing refresh
        private readonly Dictionary<SkinnedMeshRenderer, VisualElement> blendShapeGroups
            = new Dictionary<SkinnedMeshRenderer, VisualElement>();

        private Dictionary<AnimatableBlendShapeField, bool> refreshElementTracker;

        public BlendShapeContainerRenderer(
            VisualElement rootContainer, bool isFromReferenceAnimation, ConfigurationOperations configOperations)
        {
            RootContainer = rootContainer;
            this.isFromReferenceAnimation = isFromReferenceAnimation;
            this.configOperations = configOperations;

            groupVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/EZUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/BlendShapeContainer.uxml");
        }

        public VisualElement RootContainer { get; }

        public void InitializeRefresh() =>
            refreshElementTracker = RootContainer
                .Query<AnimatableBlendShapeField>()
                .ToList()
                .ToDictionary(e => e, _ => false);

        public void ProcessRefresh(SerializedProperty item, int index)
        {
            AnimatableBlendShape blendShape = ConfigSerialization.DeserializeBlendShape(item);
            VisualElement blendShapeContainer =
                GetBlendShapeGroup(blendShape).Q<VisualElement>(className: "blendshape-smr-container");

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

            foreach (KeyValuePair<SkinnedMeshRenderer, VisualElement> kvp in blendShapeGroups.ToArray())
            {
                VisualElement container = kvp.Value.Q<VisualElement>(className: "blendshape-smr-container");
                if (container.childCount == 0)
                {
                    kvp.Value.RemoveFromHierarchy();
                    _ = blendShapeGroups.Remove(kvp.Key);
                }
                else
                {
                    container.Sort((lhs, rhs) => AnimatableBlendShapeField.Compare(
                        (AnimatableBlendShapeField)lhs,
                        (AnimatableBlendShapeField)rhs));
                }
            }
        }

        private VisualElement GetBlendShapeGroup(AnimatableBlendShape blendShape)
        {
            if (blendShapeGroups.TryGetValue(blendShape.skinnedMeshRenderer, out VisualElement existingGroup))
            {
                return existingGroup;
            }

            VisualElement blendShapeGroup = groupVisualTree.CloneTree();
            blendShapeGroup.Q<EZFXLayer.UIElements.ObjectField>().value = blendShape.skinnedMeshRenderer;
            blendShapeGroups.Add(blendShape.skinnedMeshRenderer, blendShapeGroup);
            RootContainer.Add(blendShapeGroup);

            return blendShapeGroup;
        }
    }
}