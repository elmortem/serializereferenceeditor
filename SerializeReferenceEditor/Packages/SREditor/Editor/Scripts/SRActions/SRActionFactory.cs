using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class SRActionFactory
    {
        private readonly SerializedProperty _currentProperty;
        private readonly SerializedProperty _parentProperty;
        private readonly TypeInfo[] _typeInfos;

        public SRActionFactory(
            SerializedProperty currentProperty, 
            SerializedProperty parentProperty,
            TypeInfo[] typeInfos)
        {
            _currentProperty = currentProperty;
            _parentProperty = parentProperty;
            _typeInfos = typeInfos;
        }

        public InstanceClassSRAction InstantiateBuild(string type)
            => new(_currentProperty, _parentProperty, _typeInfos, type);

        public ErasePropertySRAction EraseBuild()
            => new ErasePropertySRAction(_currentProperty, _parentProperty);
    }
}