using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.MissingTypesValidator.ReportFormats
{
    [Serializable]
    public class UnityLogAssetMissingTypeReport : IAssetMissingTypeReport
    {
        private StringBuilder _stringBuilder = new();
        
        protected static string UnityObjectDescription(Object obj)
            => string.Format("Object \"{0}\" (Type: {1}, Instance: {2})",
                obj.name,
                obj.GetType().FullName,
                obj.GetInstanceID());

        protected static string MissingClassFullName(ManagedReferenceMissingType missingType)
            => string.Format("{0}.{1}, {2}", 
                missingType.namespaceName, 
                missingType.className, 
                missingType.assemblyName);

        public void AttachMissingTypes(Object missingObjectContainer, ManagedReferenceMissingType[] missingTypes)
        {
            var missingObjectContainerDescription = UnityObjectDescription(missingObjectContainer);
            _stringBuilder.Append(missingObjectContainerDescription).AppendLine();
            foreach (var missingType in missingTypes)
            {
                _stringBuilder.Append("\t").AppendFormat("{0} - {1}",
                    missingType.referenceId,
                    MissingClassFullName(missingType));
                if (missingType.serializedData.Length > 0)
                    _stringBuilder.Append("\t").AppendFormat("\n\t\t{0}", missingType.serializedData);
                _stringBuilder.AppendLine();
            }
        }

        public void Finished()
        {
            if (_stringBuilder.Length > 0)
            {
                Debug.Log(_stringBuilder.ToString());
            }
            else
            {
                Debug.Log("Not found missing types");
            }
        }
    }
}