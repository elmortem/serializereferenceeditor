using SerializeReferenceEditor.Editor.Settings;
using SerializeReferenceEditor.Services;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public static class TypeReplacer
	{
		public static bool TryUpgradeAsset(string assetPath, Object obj, out bool clearedMissingReferences)
		{
			clearedMissingReferences = false;
			if (string.IsNullOrEmpty(assetPath) && obj == null)
				return false;

			bool modified = false;
			if (!string.IsNullOrEmpty(assetPath))
			{
				foreach (var (oldAssembly, oldType, newType) in SRFormerlyTypeCache.GetAllReplacements())
				{
					var oldTypePattern = string.IsNullOrEmpty(oldAssembly) ? oldType : $"{oldAssembly}, {oldType}";
					var newAssembly = newType.Assembly.GetName().Name;
					var newTypePattern = string.IsNullOrEmpty(newAssembly) ? newType.FullName : $"{newAssembly}, {newType.FullName}";

					if (TypeReplaceHelper.ReplaceTypeInFile(assetPath, oldTypePattern, newTypePattern))
					{
						modified = true;
					}
				}
			}

			if (!modified && obj != null && SREditorSettings.GetOrCreateSettings()?.ClearMissingReferencesIfNoReplacement == true)
			{
				if (obj is GameObject gameObject)
				{
					foreach (var component in gameObject.GetComponentsInChildren<MonoBehaviour>(true))
					{
						if (component == null) continue;
						if (SerializationUtility.HasManagedReferencesWithMissingTypes(component))
						{
							SerializationUtility.ClearAllManagedReferencesWithMissingTypes(component);
							clearedMissingReferences = true;
						}
					}
				}
				else if (obj is ScriptableObject scriptable)
				{
					if (SerializationUtility.HasManagedReferencesWithMissingTypes(scriptable))
					{
						SerializationUtility.ClearAllManagedReferencesWithMissingTypes(scriptable);
						clearedMissingReferences = true;
					}
				}

				if (clearedMissingReferences)
				{
					modified = true;
				}
			}

			return modified;
		}
	}
}