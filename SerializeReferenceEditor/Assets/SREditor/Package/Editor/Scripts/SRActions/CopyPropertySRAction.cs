using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class CopyPropertySRAction : BaseSRAction
    {
        public CopyPropertySRAction(SerializedProperty currentProperty, SerializedProperty parentProperty)
            : base(currentProperty, parentProperty)
        {
        }

        protected override void DoApply()
        {
            SRClipboard.ManagedReferenceValue = Property.managedReferenceValue;
        }
    }
}
