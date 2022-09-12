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
        private readonly VisualElement container;
        private readonly SerializedProperty array;
        private readonly Func<T> elementCreator;

        public SerializedPropertyContainer(
            VisualElement container, SerializedProperty array, Func<T> elementCreator)
        {
            this.container = container;
            this.array = array;
            this.elementCreator = elementCreator;

            pathToArray = array.propertyPath;
            preUndoRedoArrayLength = array.arraySize;

            Undo.undoRedoPerformed += HandleUndo;
            container.RegisterCallback<DetachFromPanelEvent>(
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
            preUndoRedoArrayLength = array.arraySize;
            Refresh();
        }

        public void Refresh()
        {
            if (lastRefreshedChangeSequence == currentChangeSequence) return;

            ForEachProperty((i, current) =>
            {
                T element;
                if (i < container.childCount)
                {
                    element = (T)container[i];
                }
                else
                {
                    element = elementCreator();
                    container.Add(element);
                }
                element.Rebind(current);
            });

            while (container.childCount > array.arraySize)
            {
                container.RemoveAt(container.childCount - 1);
            }

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

        public IEnumerable<T> AllElements => container.Children().Cast<T>();

        public int Count => array.arraySize;

        //this is intentionally not checked for everywhere because most places are expected to be valid
        public bool IsValid => array.serializedObject.FindProperty(pathToArray) != null;
    }
}
