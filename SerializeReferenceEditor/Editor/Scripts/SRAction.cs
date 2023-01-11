using UnityEditor;

namespace SerializeReferenceEditor.Editor.Scripts
{
    public class SRAction
    {
        public SerializedProperty Property;
        public string Command;

        public SRAction(SerializedProperty p, string c)
        {
            Property = p;
            Command = c;
        }
    }
}