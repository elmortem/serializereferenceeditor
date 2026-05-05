using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class CopyPropertySRAction : BaseSRAction
    {
        public CopyPropertySRAction(SerializedProperty currentProperty)
            : base(currentProperty)
        {
        }

        protected override void DoApply()
        {
            SRClipboard.ManagedReferenceValue = Property.managedReferenceValue;
        }
    }
}
