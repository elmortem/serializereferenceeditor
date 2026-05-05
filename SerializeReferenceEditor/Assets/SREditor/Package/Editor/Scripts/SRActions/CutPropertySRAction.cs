using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class CutPropertySRAction : BaseSRAction
    {
        public CutPropertySRAction(SerializedProperty currentProperty, SerializedProperty parentProperty)
            : base(currentProperty, parentProperty)
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
