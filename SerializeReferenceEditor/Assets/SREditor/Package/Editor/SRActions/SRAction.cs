using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public abstract class BaseSRAction
    {
        protected readonly SerializedProperty Property;

        protected BaseSRAction(SerializedProperty currentProperty)
        {
            Property = currentProperty;
        }

        public void Apply()
        {
            Property.serializedObject.UpdateIfRequiredOrScript();
            DoApply();
        }

        protected abstract void DoApply();
    }
}