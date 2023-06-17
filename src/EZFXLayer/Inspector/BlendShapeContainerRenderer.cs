namespace EZUtils.EZFXLayer.UIElements
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.UIElements;

    using static Localization;

    //TODO: each renderer could probably actually be its own custom control, which would be a smoother design
    //not that the current design is bad, so not a priority
    internal class BlendShapeContainerRenderer : ISerializedPropertyContainerRenderer
    {
        private readonly IAnimatableConfigurator configurator;
        private readonly VisualTreeAsset groupVisualTree;

        //these BlendShapeContainers will be simple ones that we'll sort later when finalizing refresh
        private readonly Dictionary<SkinnedMeshRenderer, VisualElement> blendShapeGroups
            = new Dictionary<SkinnedMeshRenderer, VisualElement>();
        private VisualElement nullBlendShapeGroup;

        private Dictionary<AnimatableBlendShapeField, bool> refreshElementTracker;

        public BlendShapeContainerRenderer(VisualElement rootContainer, IAnimatableConfigurator configurator)
        {
            RootContainer = rootContainer;
            this.configurator = configurator;

            groupVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.timiz0r.ezutils.ezfxlayer/Inspector/BlendShapeContainer.uxml");
        }

        public VisualElement RootContainer { get; }

        public void InitializeRefresh()
        {
            refreshElementTracker = RootContainer
                .Query<AnimatableBlendShapeField>()
                .ToList()
                .ToDictionary(e => e, _ => false);

        }

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
                AnimatableBlendShapeField newElement = new AnimatableBlendShapeField(configurator, item);
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

            if (nullBlendShapeGroup != null
                && nullBlendShapeGroup.Q<VisualElement>(className: "blendshape-smr-container") is VisualElement nullSmrContainer)
            {
                if (nullSmrContainer.childCount == 0)
                {
                    nullBlendShapeGroup.RemoveFromHierarchy();
                    nullBlendShapeGroup = null;
                }
                else
                {
                    nullSmrContainer.Sort((lhs, rhs) => AnimatableBlendShapeField.Compare(
                        (AnimatableBlendShapeField)lhs,
                        (AnimatableBlendShapeField)rhs));
                }
            }
        }

        private VisualElement GetBlendShapeGroup(AnimatableBlendShape blendShape)
        {
            //we do a null check on the skinnedMeshRenderer property in case the reference is no longer valid
            if (!(blendShape.skinnedMeshRenderer is SkinnedMeshRenderer smr))
            {
                if (nullBlendShapeGroup == null)
                {
                    nullBlendShapeGroup = CreateBlendShapeGroup(null);
                    RootContainer.Add(nullBlendShapeGroup);
                }
                return nullBlendShapeGroup;
            }

            if (blendShapeGroups.TryGetValue(smr, out VisualElement existingGroup))
            {
                return existingGroup;
            }

            VisualElement blendShapeGroup = CreateBlendShapeGroup(blendShape.skinnedMeshRenderer);
            blendShapeGroups.Add(blendShape.skinnedMeshRenderer, blendShapeGroup);
            RootContainer.Add(blendShapeGroup);

            return blendShapeGroup;
        }

        private VisualElement CreateBlendShapeGroup(SkinnedMeshRenderer smr)
        {
            VisualElement blendShapeGroup = groupVisualTree.CloneTree();
            TranslateElementTree(blendShapeGroup);

            VisualElement objectFieldContainer =
                blendShapeGroup.Q<VisualElement>(className: "blendshape-smr-objectfield-container");
            EZUtils.EZFXLayer.UIElements.ObjectField objectField =
                objectFieldContainer.Q<EZUtils.EZFXLayer.UIElements.ObjectField>();
            objectFieldContainer.RegisterCallback<MouseDownEvent>(mouseUpEvent =>
            {
                if (!(objectField.value is Object targetObject)) return;

                if (mouseUpEvent.clickCount == 1)
                {
                    EditorGUIUtility.PingObject(targetObject);
                }
                else if (mouseUpEvent.clickCount == 2)
                {
                    _ = AssetDatabase.OpenAsset(targetObject);
                }
            });

            blendShapeGroup.Q<EZFXLayer.UIElements.ObjectField>().value = smr;

            return blendShapeGroup;
        }
    }
}
