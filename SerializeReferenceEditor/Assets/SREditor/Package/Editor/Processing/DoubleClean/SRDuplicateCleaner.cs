using System;
using System.Collections.Generic;
using System.Reflection;
using SerializeReferenceEditor.Editor.Settings;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.Processing.DoubleClean
{
    public static class SRDuplicateCleaner
    {
        public static bool TryCleanupObject(Object asset, SRDuplicateMode duplicateMode)
        {
            if (asset == null)
                return false;

            bool anyChanged = false;
            var seenObjects = new HashSet<object>();

            if (asset is GameObject gameObject)
            {
                foreach (var component in gameObject.GetComponents<Component>())
                {
                    if (component == null)
                        continue;
                    anyChanged |= ProcessSerializedObject(new SerializedObject(component), duplicateMode, seenObjects);
                }
            }
            else
            {
                try
                {
                    anyChanged |= ProcessSerializedObject(new SerializedObject(asset), duplicateMode, seenObjects);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            return anyChanged;
        }

        private static bool ProcessSerializedObject(SerializedObject serializedObject, SRDuplicateMode duplicateMode, HashSet<object> seenObjects)
        {
            bool changed = false;
            serializedObject.UpdateIfRequiredOrScript();

            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.Next(enterChildren))
            {
                enterChildren = true;
                if (iterator.propertyType != SerializedPropertyType.ManagedReference)
                    continue;

                var managedReferenceValue = iterator.managedReferenceValue;
                if (managedReferenceValue == null)
                    continue;

                if (!seenObjects.Add(managedReferenceValue))
                {
                    bool refChanged = false;
                    switch (duplicateMode)
                    {
                        case SRDuplicateMode.Null:
                            iterator.managedReferenceValue = null;
                            refChanged = true;
                            break;

                        case SRDuplicateMode.Default:
                            refChanged = ApplyDefaultValue(iterator);
                            break;

                        case SRDuplicateMode.Copy:
                            refChanged = ApplyDeepCopy(iterator, managedReferenceValue, seenObjects);
                            break;
                    }

                    if (refChanged)
                        changed = true;
                }
            }

            if (changed)
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return changed;
        }

        private static bool ApplyDefaultValue(SerializedProperty property)
        {
            var currentValue = property.managedReferenceValue;
            if (currentValue == null)
            {
                property.managedReferenceValue = null;
                return true;
            }

            var propertyPath = property.propertyPath;
            bool isArrayElement = propertyPath.EndsWith("]");
            if (!isArrayElement)
            {
                var parentProperty = property.serializedObject.FindProperty(propertyPath.Substring(0, propertyPath.LastIndexOf('.')));
                if (parentProperty != null)
                {
                    object parentObject = null;
                    if (parentProperty.propertyType == SerializedPropertyType.ManagedReference)
                        parentObject = parentProperty.managedReferenceValue;
                    else if (parentProperty.propertyType == SerializedPropertyType.ObjectReference)
                        parentObject = parentProperty.objectReferenceValue;
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
                            return true;
                        }
                    }
                }
            }

            // Array/list element: create new instance and initialize fields with defaults
            var currentType = currentValue.GetType();
            object newInstance;
            try
            {
                newInstance = Activator.CreateInstance(currentType);
            }
            catch
            {
                property.managedReferenceValue = null;
                return true;
            }

            foreach (var field in currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                field.SetValue(newInstance, GetDefaultValueFromField(field));
            }

            property.managedReferenceValue = newInstance;
            return true;
        }

        private static bool ApplyDeepCopy(SerializedProperty property, object managedReferenceValue, HashSet<object> seenObjects)
        {
            var sourceType = managedReferenceValue?.GetType();
            try
            {
                var newInstance = CreateDeepCopy(managedReferenceValue);
                property.managedReferenceValue = newInstance;
                if (newInstance != null)
                    seenObjects.Add(newInstance);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create instance of type {sourceType?.Name}: {e.Message}");
                property.managedReferenceValue = null;
                return true;
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