using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class ErasePropertySRAction : BaseSRAction
    {
        public ErasePropertySRAction(SerializedProperty currentProperty)
            : base(currentProperty)
        {

        }

        protected override void DoApply()
        {
            Undo.RegisterCompleteObjectUndo(Property.serializedObject.targetObject, "Erase element");
            Undo.FlushUndoRecordObjects();

            Property.managedReferenceValue = null;
            Property.serializedObject.ApplyModifiedProperties();
        }
    }
}