namespace EZFXLayer.UIElements
{
    using System;
    using System.Linq;
    using EZFXLayer;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal class AnimatableGameObjectField : BindableElement, ISerializedPropertyContainerItem
    {
        private readonly ConfigurationOperations configOperations;
        private readonly bool isFromReferenceAnimation;

        public AnimatableGameObject GameObject { get; private set; }

        public AnimatableGameObjectField(
            ConfigurationOperations configOperations,
            bool isFromReferenceAnimation)
        {
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/TimiUtils/EZFXLayer/src/EZFXLayer.Editor/Inspector/Controls/AnimatableGameObjectField.uxml");
            visualTree.CloneTree(this);

            this.configOperations = configOperations;
            this.isFromReferenceAnimation = isFromReferenceAnimation;

            if (isFromReferenceAnimation)
            {
                this.Q<UnityEngine.UIElements.Button>().clicked += () => this.configOperations.RemoveGameObject(GameObject);
            }

            _ = this.Q<Toggle>().RegisterValueChangedCallback(evt =>
            {
                GameObject.active = evt.newValue;

                if (isFromReferenceAnimation)
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

        public static int Compare(AnimatableBlendShapeField lhs, AnimatableBlendShapeField rhs)
        {
            string[] blendShapeNames = lhs.BlendShape.skinnedMeshRenderer.GetBlendShapeNames().ToArray();
            int result =
                Array.IndexOf(blendShapeNames, lhs.BlendShape.name)
                - Array.IndexOf(blendShapeNames, rhs.BlendShape.name);
            return result;
        }

        public void Rebind(SerializedProperty serializedProperty)
        {
            GameObject = ConfigSerialization.DeserializeGameObject(serializedProperty);
            //note that under some circumstances (including redos), a rebind will trigger the toggle's
            //RegisterValueChangedCallback, so we want the GameObject it uses to be initialized first
            this.BindProperty(serializedProperty);

            if (!isFromReferenceAnimation)
            {
                CheckForReferenceMatch();
            }
        }
    }
}
