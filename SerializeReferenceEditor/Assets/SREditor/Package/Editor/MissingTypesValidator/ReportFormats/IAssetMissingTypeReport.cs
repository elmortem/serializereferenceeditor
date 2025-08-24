using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.MissingTypesValidator.ReportFormats
{
    [Obsolete("IAssetMissingTypeReport is deprecated. Use Tools/SREditor/Log MissingTypes instead.")]
    public interface IAssetMissingTypeReport
    {
        void AttachMissingTypes(Object missingObjectContainer, ManagedReferenceMissingType[] missingTypes);
        void Finished();
    }
}