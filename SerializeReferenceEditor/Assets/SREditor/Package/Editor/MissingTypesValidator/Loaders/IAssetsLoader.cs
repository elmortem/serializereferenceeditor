using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.MissingTypesValidator.Loaders
{
	[Obsolete("IAssetsLoader is deprecated. Use Tools/SREditor/Log MissingTypes instead.")]
    public interface IAssetsLoader
    {
        bool TryLoadAssetsForCheck([NotNull] List<Object> assets);
    }
}