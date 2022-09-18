namespace EZUtils.EZFXLayer.UIElements
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEngine.UIElements;

    internal class SerializedPropertyContainer
    {
        private int currentChangeSequence;
        private int lastRefreshedChangeSequence = -1;
        private int preUndoRedoArrayLength;

        private readonly string pathToArray;
        private readonly SerializedProperty array;
        private readonly ISerializedPropertyContainerRenderer renderer;

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

        public static SerializedPropertyContainer CreateSimple<T>(
            VisualElement container, SerializedProperty array, Func<T> elementCreator)
            where T : BindableElement, ISerializedPropertyContainerItem
            => new SerializedPropertyContainer(array, new SimpleSerializedPropertyContainerRenderer<T>(container, elementCreator));

        //TODO
        //a bit of a leaky implementation detail
        //ideally would do something like  MarkComplete or Finalize, but then we'd want to make the rest of the instance
        //unusable when that happens, and don't feel like putting in the work yet
        public void StopUndoRedoHandling() => Undo.undoRedoPerformed -= HandleUndo;

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

            renderer.InitializeRefresh();

            ForEachProperty((i, current) => renderer.ProcessRefresh(current, index: i));

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

        public void ForEachProperty(Action<int, SerializedProperty> action) => array.ForEachArrayElement(action);

        public void ForEachProperty(Action<SerializedProperty> action) => array.ForEachArrayElement(action);

        public IEnumerable<SerializedProperty> AllProperties => array.GetArrayElements();

        public IEnumerable<T> AllElements<T>() where T : VisualElement
            => renderer.RootContainer.Query<T>().ToList();

        public int Count => array.arraySize;

        //this is intentionally not checked for everywhere because most places are expected to be valid
        public bool IsValid => array.serializedObject.FindProperty(pathToArray) != null;

        //TODO: salso, change that one interface back to IRebindable, which will only be used by SimpleSerializedPropertyContainerRenderer
        //could call it ISimpleSerializedPropertyContainerItem? not sure how i feel about that
        private class SimpleSerializedPropertyContainerRenderer<T> : ISerializedPropertyContainerRenderer
            where T : BindableElement, ISerializedPropertyContainerItem
        {
            private readonly Func<T> elementCreator;

            public SimpleSerializedPropertyContainerRenderer(VisualElement rootContainer, Func<T> elementCreator)
            {
                RootContainer = rootContainer;
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

            public void InitializeRefresh() { }
        }
    }

    public interface ISerializedPropertyContainerRenderer
    {
        VisualElement RootContainer { get; }
        //could instead get the index by getting it from the propertypath (.Array.item[69])
        //while makes the design of this interface prettier, really who cares
        void ProcessRefresh(SerializedProperty item, int index);
        void FinalizeRefresh(SerializedProperty array);
        void InitializeRefresh();
    }
}
