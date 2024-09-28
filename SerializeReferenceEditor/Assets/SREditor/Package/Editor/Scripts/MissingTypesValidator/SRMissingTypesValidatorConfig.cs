using System;
using SerializeReferenceEditor.Editor.MissingTypesValidator.Loaders;
using SerializeReferenceEditor.Editor.MissingTypesValidator.ReportFormats;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.MissingTypesValidator
{
    [CreateAssetMenu(fileName = "SRMissingTypesValidatorConfig",
        menuName = "Tools/SREditor/SRMissingTypesValidatorConfig",
        order = 0)]
    public class SRMissingTypesValidatorConfig : ScriptableObject
    {
        public AssetChecker[] Checkers;
    }
    
    [Serializable]
    public class AssetChecker
    {
        [SR, SerializeReference] 
        public IAssetsLoader AssetsLoaders;
        [SR, SerializeReference] 
        public IAssetMissingTypeReport ReportType = new UnityLogAssetMissingTypeReport();
    }
}