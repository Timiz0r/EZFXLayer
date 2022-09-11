namespace EZFXLayer.UIElements
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    public class AnimationConfigurationField : BindableElement
    {
        private readonly VisualElement blendShapeContainer;
        private readonly SerializedProperty blendShapes;
        private readonly bool canModify;
        private SerializedProperty gameObjects;

        public AnimationConfigurationField(SerializedProperty serializedProperty, bool canModify)
        {
            this.BindProperty(serializedProperty);
            this.canModify = canModify;

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
                    blendShapes.arraySize++;
                    SerializedProperty newBlendShape = blendShapes.GetArrayElementAtIndex(blendShapes.arraySize - 1);
                    newBlendShape.FindPropertyRelative("name").stringValue = "florpy";
                    newBlendShape.FindPropertyRelative("value").floatValue = 100;
                    _ = blendShapes.serializedObject.ApplyModifiedProperties();
                    Refresh();
                };
            }

            //the attribute-based way isn't working properly, so we'll keep on doing this for now
            FoldoutWithContainer foldout = this.Q<FoldoutWithContainer>();
            VisualElement foldoutContent = this.Q<VisualElement>(name: "foldoutContent");
            foldout.ConfigureSeparateContainer(foldoutContent);

            blendShapeContainer = foldoutContent.Q<VisualElement>(name: "blendShapes");
            blendShapes = serializedProperty.FindPropertyRelative("blendShapes");

            VisualElement gameObjectContainer = foldoutContent.Q<VisualElement>(name: "gameObjects");

            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < blendShapes.arraySize; i++)
            {
                SerializedProperty blendShape = blendShapes.GetArrayElementAtIndex(i);

                AnimatableBlendShapeField blendShapeField;
                if (i < blendShapeContainer.childCount)
                {
                    blendShapeField = (AnimatableBlendShapeField)blendShapeContainer[i];
                }
                else
                {
                    blendShapeField = new AnimatableBlendShapeField(canModify);
                }
                blendShapeField.Reconfigure(() => DeleteBlendShape(blendShape));
                blendShapeField.BindProperty(blendShape);
                blendShapeContainer.Add(blendShapeField);
            }
            while (blendShapeContainer.childCount > blendShapes.arraySize)
            {
                blendShapeContainer.RemoveAt(blendShapeContainer.childCount - 1);
            }
        }

        private void DeleteBlendShape(SerializedProperty blendShape)
        {
            _ = blendShape.DeleteCommand();
            _ = blendShape.serializedObject.ApplyModifiedProperties();
            // element.RemoveFromHierarchy();
            Refresh();
        }
    }
}
