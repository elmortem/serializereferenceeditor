using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class SRActionFactory
    {
        private readonly SerializedProperty _currentProperty;
        private readonly SerializedProperty _parentProperty;
        private readonly SRAttribute _srAttribute;

        public SRActionFactory(
            SerializedProperty currentProperty, 
            SerializedProperty parentProperty,
            SRAttribute srAttribute)
        {
            _currentProperty = currentProperty;
            _parentProperty = parentProperty;
            _srAttribute = srAttribute;
        }

        public InstanceClassSRAction InstantiateBuild(string type)
            => new(_currentProperty, _parentProperty, _srAttribute, type);

        public ErasePropertySRAction EraseBuild()
            => new ErasePropertySRAction(_currentProperty, _parentProperty);
    }
}