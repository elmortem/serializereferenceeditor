using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.MissingTypesValidator.Loaders
{
    [Serializable]
    [SRName("Load all ScriptableObjects")]
    public class LoadAllScriptableObjects : IAssetsLoader
    {
        public bool TryLoadAssetsForCheck(List<Object> assets)
        {
            if (assets == null) 
                throw new ArgumentNullException(nameof(assets));
            
            var serializedObjects =  Resources.FindObjectsOfTypeAll<ScriptableObject>();
            assets.AddRange(serializedObjects);
            return assets.Count > 0;
        }
    }
}