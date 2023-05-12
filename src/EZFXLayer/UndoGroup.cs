namespace EZUtils.EZFXLayer
{
    using System;
    using UnityEditor;

    public class UndoGroup : IDisposable
    {
        private readonly string groupName;
        private readonly int undoGroup;
        private string failedGroupName = null;

        public UndoGroup(string groupName)
        {
            this.groupName = groupName;
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
        }

        public void MarkFailure(string groupName) => failedGroupName = groupName;

        public void Dispose()
        {
            Undo.SetCurrentGroupName(failedGroupName ?? groupName);
            Undo.CollapseUndoOperations(undoGroup);
        }
    }
}
