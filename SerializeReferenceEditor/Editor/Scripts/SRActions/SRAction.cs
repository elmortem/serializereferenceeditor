using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public abstract class BaseSRAction
    {
        protected readonly SerializedProperty Property;
        private readonly SerializedProperty _parentProperty;

        protected BaseSRAction(SerializedProperty currentProperty, SerializedProperty parentProperty)
        {
            Property = currentProperty;
            _parentProperty = parentProperty;
        }

        public void Apply()
        {
            Property.serializedObject.UpdateIfRequiredOrScript();
            _parentProperty?.serializedObject.UpdateIfRequiredOrScript();
            DoApply();
        }

        protected abstract void DoApply();
    }
}