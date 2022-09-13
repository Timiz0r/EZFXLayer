namespace EZFXLayer.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    internal class SerializedPropertyContainer<T> where T : BindableElement, ISerializedPropertyContainerItem
    {
        private int currentChangeSequence;
        private int lastRefreshedChangeSequence = -1;
        private int preUndoRedoArrayLength;

        private readonly string pathToArray;
        private readonly SerializedProperty array;
        private readonly ISerializedPropertyContainerRenderer renderer;

        public SerializedPropertyContainer(VisualElement container, SerializedProperty array, Func<T> elementCreator)
            : this(array, new SimpleSerializedPropertyContainerRenderer<T>(container, elementCreator))
        {

        }

        public SerializedPropertyContainer(SerializedProperty array, ISerializedPropertyContainerRenderer renderer)
        {
            this.array = array;
            this.renderer = renderer;

            pathToArray = array.propertyPath;
            preUndoRedoArrayLength = array.arraySize;

            Undo.undoRedoPerformed += HandleUndo;
            renderer.RootContainer.RegisterCallback<DetachFromPanelEvent>(
                evt =>
                    Undo.undoRedoPerformed -= HandleUndo);
        }

        private void HandleUndo()
        {
            array.serializedObject.Update();
            if (!IsValid) return;

            if (preUndoRedoArrayLength != array.arraySize)
            {
                preUndoRedoArrayLength = array.arraySize;
                currentChangeSequence++;
            }

            Refresh();
        }

        public void RefreshExternalChanges()
        {
            currentChangeSequence++;
            Refresh();
        }

        public void Refresh()
        {
            if (lastRefreshedChangeSequence == currentChangeSequence) return;

            ForEachProperty((i, current) =>
            {
                renderer.ProcessRefresh(current, index: i);
            });

            renderer.FinalizeRefresh(array);

            lastRefreshedChangeSequence = currentChangeSequence;
            preUndoRedoArrayLength = array.arraySize;
        }

        public SerializedProperty Add(Action<SerializedProperty> initializer, bool apply = true)
        {
            array.arraySize++;
            SerializedProperty newProperty = array.GetArrayElementAtIndex(array.arraySize - 1);
            initializer(newProperty);

            preUndoRedoArrayLength++;
            currentChangeSequence++;

            if (apply)
            {
                _ = array.serializedObject.ApplyModifiedProperties();
            }

            Refresh();

            return newProperty;
        }

        public void Remove(Func<SerializedProperty, bool> predicate, bool apply = true)
        {
            //reverse because DeleteCommand shifts elements
            //toarray because DeleteCommand changes the count
            SerializedProperty[] all = AllProperties.Reverse().ToArray();
            foreach (SerializedProperty sp in all)
            {
                if (!predicate(sp)) continue;
                _ = sp.DeleteCommand();
            }
            preUndoRedoArrayLength -= all.Length;
            currentChangeSequence++;

            if (apply)
            {
                _ = array.serializedObject.ApplyModifiedProperties();
            }

            Refresh();
        }

        public void ForEachProperty(Action<int, SerializedProperty> action)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty current = array.GetArrayElementAtIndex(i);

                action(i, current);
            }
        }

        public void ForEachProperty(Action<SerializedProperty> action) => ForEachProperty((i, sp) => action(sp));

        public IEnumerable<SerializedProperty> AllProperties
            => Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i));

        public void ForEachElement(Action<T> action)
        {
            foreach (T element in AllElements)
            {
                action(element);
            }
        }

        public IEnumerable<T> AllElements => renderer.RootContainer.Query<T>().ToList();

        public int Count => array.arraySize;

        //this is intentionally not checked for everywhere because most places are expected to be valid
        public bool IsValid => array.serializedObject.FindProperty(pathToArray) != null;
    }

    public interface ISerializedPropertyContainerRenderer
    {
        VisualElement RootContainer { get; }
        void ProcessRefresh(SerializedProperty item, int index);
        void FinalizeRefresh(SerializedProperty array);
    }

    public class SimpleSerializedPropertyContainerRenderer<T> : ISerializedPropertyContainerRenderer
        where T : BindableElement, ISerializedPropertyContainerItem
    {
        private readonly Func<T> elementCreator;

        public SimpleSerializedPropertyContainerRenderer(VisualElement container, Func<T> elementCreator)
        {
            RootContainer = container;
            this.elementCreator = elementCreator;
        }

        public VisualElement RootContainer { get; }

        public void ProcessRefresh(SerializedProperty item, int index)
        {
            T element;
            if (index < RootContainer.childCount)
            {
                element = (T)RootContainer[index];
            }
            else
            {
                element = elementCreator();
                RootContainer.Add(element);
            }
            element.Rebind(item);
        }

        public void FinalizeRefresh(SerializedProperty array)
        {
            while (RootContainer.childCount > array.arraySize)
            {
                RootContainer.RemoveAt(RootContainer.childCount - 1);
            }
        }
    }
}
