using UnityEditor;

namespace SerializeReferenceEditor.Editor.SRActions
{
    public class SRActionFactory
    {
        private readonly SerializedProperty _currentProperty;
        private readonly TypeInfo[] _typeInfos;

        public SRActionFactory(
            SerializedProperty currentProperty,
            TypeInfo[] typeInfos)
        {
            _currentProperty = currentProperty;
            _typeInfos = typeInfos;
        }

        public InstanceClassSRAction InstantiateBuild(string type)
            => new(_currentProperty, _typeInfos, type);

        public ErasePropertySRAction EraseBuild()
            => new ErasePropertySRAction(_currentProperty);

        public CopyPropertySRAction CopyBuild()
            => new CopyPropertySRAction(_currentProperty);

        public PastePropertySRAction PasteBuild()
            => new PastePropertySRAction(_currentProperty);

        public CutPropertySRAction CutBuild()
            => new CutPropertySRAction(_currentProperty);
    }
}
