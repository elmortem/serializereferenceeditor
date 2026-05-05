using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class PastePropertySRAction : BaseSRAction
    {
        public PastePropertySRAction(SerializedProperty currentProperty)
            : base(currentProperty)
        {
        }

        protected override void DoApply()
        {
            if (!SRClipboard.HasValue)
                return;

            Undo.RegisterCompleteObjectUndo(Property.serializedObject.targetObject, "Paste element");
            Undo.FlushUndoRecordObjects();

            Property.managedReferenceValue = SRClipboard.ManagedReferenceValue;
            Property.serializedObject.ApplyModifiedProperties();
        }
    }
}
