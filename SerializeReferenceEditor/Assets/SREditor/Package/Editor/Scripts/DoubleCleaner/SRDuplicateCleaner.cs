using System.Collections.Generic;
using System;
using System.Reflection;
using SerializeReferenceEditor.Editor.Settings;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.DoubleCleaner
{
	[InitializeOnLoad]
	public static class SRDuplicateCleaner
	{
		static SRDuplicateCleaner()
		{
			AssetChangeDetector.ChangeEvent -= OnAssetChanged;
			AssetChangeDetector.ChangeEvent += OnAssetChanged;
		}
			
		private static void OnAssetChanged(Object asset)
		{
			if (asset == null)
				return;

			SRDuplicateMode duplicateMode = SREditorSettings.GetOrCreateSettings()?.DuplicateMode ?? SRDuplicateMode.Null;

			var seenObjects = new HashSet<object>();

			if (asset is GameObject gameObject)
			{
				foreach (var component in gameObject.GetComponents<Component>())
				{
					if (component == null)
						continue;

					var serializedObject = new SerializedObject(component);
					var iterator = serializedObject.GetIterator();
					ProcessSerializedProperty(iterator, duplicateMode, seenObjects);
				}
			}
			else
			{
				try
				{
					var serializedObject = new SerializedObject(asset);
					var iterator = serializedObject.GetIterator();
					ProcessSerializedProperty(iterator, duplicateMode, seenObjects);
				}
				catch (Exception ex)
				{
					Debug.LogError(ex);
				}
			}
		}
	
		private static void ProcessSerializedProperty(SerializedProperty property, SRDuplicateMode duplicateMode, HashSet<object> seenObjects)
		{
			while (property.Next(true))
			{
				if (property.propertyType == SerializedPropertyType.ManagedReference)
				{
					var managedReferenceValue = property.managedReferenceValue;
					
					if (managedReferenceValue != null && !seenObjects.Add(managedReferenceValue))
					{
						var refChanged = false;
						switch (duplicateMode)
						{
							case SRDuplicateMode.Null:
								property.managedReferenceValue = null;
								refChanged = true;
								break;

							case SRDuplicateMode.Default:
								var currentValue = property.managedReferenceValue;
								if (currentValue == null)
								{
									property.managedReferenceValue = null;
									refChanged = true;
									break;
								}

								var propertyPath = property.propertyPath;
								bool isArrayElement = propertyPath.EndsWith("]");
								
								if (!isArrayElement)
								{
									var parentProperty =
										property.serializedObject.FindProperty(
											propertyPath.Substring(0, propertyPath.LastIndexOf('.')));
									
									if (parentProperty != null)
									{
										object parentObject = null;

										if (parentProperty.propertyType == SerializedPropertyType.ManagedReference)
										{
											parentObject = parentProperty.managedReferenceValue;
										}
										else if (parentProperty.propertyType == SerializedPropertyType.ObjectReference)
										{
											parentObject = parentProperty.objectReferenceValue;
										}
										else if (parentProperty.propertyType == SerializedPropertyType.Generic)
										{
											var targetObject = parentProperty.serializedObject.targetObject;
											var parentPath = propertyPath.Substring(0, propertyPath.LastIndexOf('.'));
											parentObject = GetObjectFromPath(targetObject, parentPath);
										}

										if (parentObject != null)
										{
											var parentType = parentObject.GetType();
											var fieldName = propertyPath.Substring(propertyPath.LastIndexOf('.') + 1);
											var field = parentType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
											
											if (field != null)
											{
												property.managedReferenceValue = GetDefaultValueFromField(field);
												refChanged = true;
											}
											else
											{
												property.managedReferenceValue = null;
												refChanged = true;
											}
										}
										else
										{
											property.managedReferenceValue = null;
											refChanged = true;
										}
									}
									else
									{
										property.managedReferenceValue = null;
										refChanged = true;
									}
								}
								else
								{
									var currentType = currentValue.GetType();
									var newInstance = Activator.CreateInstance(currentType);
									
									foreach (var field in currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
									{
										field.SetValue(newInstance, GetDefaultValueFromField(field));
									}
									
									property.managedReferenceValue = newInstance;
									refChanged = true;
								}
								break;

							case SRDuplicateMode.Copy:
								var sourceType = managedReferenceValue.GetType();
								try
								{
									var newInstance = CreateDeepCopy(managedReferenceValue);
									property.managedReferenceValue = newInstance;
									seenObjects.Add(newInstance);
									refChanged = true;
								}
								catch (Exception e)
								{
									Debug.LogError($"Failed to create instance of type {sourceType.Name}: {e.Message}");
									property.managedReferenceValue = null;
									refChanged = true;
								}
								break;
						}
						
						if (refChanged)
							property.serializedObject.ApplyModifiedProperties();
					}
				}
			}
		}

		private static object GetDefaultValueFromField(FieldInfo field)
		{
			try
			{
				if (field.GetCustomAttribute<SerializeReference>() != null)
				{
					if (field.DeclaringType != null)
					{
						var tempInstance = Activator.CreateInstance(field.DeclaringType);
						var defaultValue = field.GetValue(tempInstance);

						if (defaultValue != null)
						{
							var defaultType = defaultValue.GetType();
							var newInstance = Activator.CreateInstance(defaultType);
						
							foreach (var f in defaultType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
							{
								var value = f.GetValue(defaultValue);
								if (value != null)
								{
									f.SetValue(newInstance, value);
								}
							}
						
							return newInstance;
						}
					}
				}
				
				var fieldType = field.FieldType;
				if (fieldType.IsArray)
				{
					var elementType = fieldType.GetElementType();
					if (elementType != null)
						return Array.CreateInstance(elementType, 0);
					
					return null;
				}
				
				if (typeof(System.Collections.IList).IsAssignableFrom(fieldType) && fieldType.IsGenericType) 
				{ 
					return Activator.CreateInstance(fieldType);
				}
				
				return null;
			}
			catch (Exception e)
			{
				Debug.LogError($"Failed to get default value for field {field.Name}: {e.Message}");
				return null;
			}
		}

		private static object CreateDeepCopy(object source)
		{
			if (source == null) return null;
			
			var sourceType = source.GetType();
			var newInstance = Activator.CreateInstance(sourceType);
			
			foreach (var field in sourceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				var value = field.GetValue(source);
				if (value == null)
				{
					field.SetValue(newInstance, null);
					continue;
				}

				var isSerializeReference = field.GetCustomAttribute<SerializeReference>() != null;
				
				if (isSerializeReference)
				{
					var copiedValue = CreateDeepCopy(value);
					field.SetValue(newInstance, copiedValue);
				}
				else
				{
					field.SetValue(newInstance, value);
				}
			}
			
			return newInstance;
		}

		private static object GetObjectFromPath(object root, string path)
		{
			if (root == null || string.IsNullOrEmpty(path))
				return null;

			var parts = path.Split('.');
			object current = root;

			for (int i = 0; i < parts.Length; i++)
			{
				if (current == null)
					return null;

				var part = parts[i];

				if (part == "Array" && i + 1 < parts.Length && parts[i + 1].StartsWith("data["))
				{
					var arrayIndexPart = parts[i + 1];
					var indexStr = arrayIndexPart.Substring(5, arrayIndexPart.Length - 6);
					if (int.TryParse(indexStr, out int index))
					{
						var array = current as Array;
						if (array != null && index >= 0 && index < array.Length)
						{
							current = array.GetValue(index);
						}
						else
						{
							return null;
						}
					}
					i++;
					continue;
				}

				var field = current.GetType().GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (field == null)
					return null;

				current = field.GetValue(current);
			}

			return current;
		}
	}
}