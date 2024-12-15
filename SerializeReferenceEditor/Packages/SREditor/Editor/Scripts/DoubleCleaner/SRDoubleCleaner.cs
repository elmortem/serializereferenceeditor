using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.DoubleCleaner
{
	[InitializeOnLoad]
	public static class SRDoubleCleaner
	{
		static SRDoubleCleaner()
		{
			AssetChangeDetector.ChangeEvent -= OnAssetChanged;
			AssetChangeDetector.ChangeEvent += OnAssetChanged;
		}
			
		private static void OnAssetChanged(Object asset)
		{
			if (asset == null)
				return;

			var seenObjects = new HashSet<object>();

			if (asset is GameObject gameObject)
			{
				foreach (var component in gameObject.GetComponents<Component>())
				{
					if (component == null)
						continue;

					var serializedObject = new SerializedObject(component);
					var iterator = serializedObject.GetIterator();
					ProcessSerializedProperty(iterator, seenObjects);
				}
			}
			else
			{
				var serializedObject = new SerializedObject(asset);
				var iterator = serializedObject.GetIterator();
				ProcessSerializedProperty(iterator, seenObjects);
			}
		}
	
		private static void ProcessSerializedProperty(SerializedProperty property, HashSet<object> seenObjects)
		{
			while (property.Next(true))
			{
				if (property.propertyType == SerializedPropertyType.ManagedReference)
				{
					var managedReferenceValue = property.managedReferenceValue;
					if (managedReferenceValue != null && !seenObjects.Add(managedReferenceValue))
					{
						property.managedReferenceValue = null;
						property.serializedObject.ApplyModifiedProperties();
					}
				}
			}
		}
	}
}