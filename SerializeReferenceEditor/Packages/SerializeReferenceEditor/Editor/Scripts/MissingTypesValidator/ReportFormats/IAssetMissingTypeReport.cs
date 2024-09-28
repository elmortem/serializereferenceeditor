using UnityEditor;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.MissingTypesValidator.ReportFormats
{
    public interface IAssetMissingTypeReport
    {
        void AttachMissingTypes(Object missingObjectContainer, ManagedReferenceMissingType[] missingTypes);
        void Finished();
    }
}