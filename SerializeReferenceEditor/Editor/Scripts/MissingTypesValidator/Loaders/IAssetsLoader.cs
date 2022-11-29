using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.Scripts.MissingTypesValidator.Loaders
{
    public interface IAssetsLoader
    {
        bool TryLoadAssetsForCheck([NotNull] List<Object> assets);
    }
}