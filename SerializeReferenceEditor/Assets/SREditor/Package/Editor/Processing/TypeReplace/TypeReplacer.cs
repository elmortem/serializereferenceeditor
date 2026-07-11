using SerializeReferenceEditor.Editor.Settings;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public static class TypeReplacer
	{
		public static bool TryClearMissingReferences(Object obj, out bool clearedMissingReferences)
		{
			clearedMissingReferences = false;

			if (obj == null)
				return false;

			if (SREditorSettings.GetOrCreateSettings()?.ClearMissingReferencesIfNoReplacement != true)
				return false;

			if (obj is GameObject gameObject)
			{
				foreach (var component in gameObject.GetComponentsInChildren<MonoBehaviour>(true))
				{
					if (component == null)
						continue;

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

			return clearedMissingReferences;
		}

		public static bool ClearMissingOn(Object target)
		{
			if (target == null)
				return false;

			if (!SerializationUtility.HasManagedReferencesWithMissingTypes(target))
				return false;

			SerializationUtility.ClearAllManagedReferencesWithMissingTypes(target);
			return true;
		}
	}
}
