using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class CutPropertySRAction : BaseSRAction
    {
        public CutPropertySRAction(SerializedProperty currentProperty)
            : base(currentProperty)
        {
        }

        protected override void DoApply()
        {
            Undo.RegisterCompleteObjectUndo(Property.serializedObject.targetObject, "Cut element");
            Undo.FlushUndoRecordObjects();

            SRClipboard.ManagedReferenceValue = Property.managedReferenceValue;
            Property.managedReferenceValue = null;
            Property.serializedObject.ApplyModifiedProperties();
        }
    }
}
