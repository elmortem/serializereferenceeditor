using System;
using SerializeReferenceEditor.Editor.MissingTypesValidator.Loaders;
using SerializeReferenceEditor.Editor.MissingTypesValidator.ReportFormats;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.MissingTypesValidator
{
    [CreateAssetMenu(fileName = "SRMissingTypesValidatorConfig",
        menuName = "SREditor/[Deprecated] SRMissingTypesValidatorConfig",
        order = 0)]
    [Obsolete("SRMissingTypesValidatorConfig is deprecated. Use Tools/SREditor/Log MissingTypes instead.")]
    public class SRMissingTypesValidatorConfig : ScriptableObject
    {
        public AssetChecker[] Checkers;
    }
    
    [Serializable]
    [Obsolete("AssetChecker is deprecated with SRMissingTypesValidator. Use Tools/SREditor/Log MissingTypes instead.")]
    public class AssetChecker
    {
        [SR, SerializeReference] 
        public IAssetsLoader AssetsLoaders;
        [SR, SerializeReference] 
        public IAssetMissingTypeReport ReportType = new UnityLogAssetMissingTypeReport();
    }
}