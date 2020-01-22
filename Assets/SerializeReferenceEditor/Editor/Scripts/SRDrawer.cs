using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(SRAttribute))]
public class SRDrawer : PropertyDrawer
{
	private SRAttribute _attr;
	private SerializedProperty _element;
	private SerializedProperty _array;
	private int _elementIndex;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if(!property.isArray)
		{
			if(_element == null)
			{
				_element = property;
				_array = GetParentArray(property, out _elementIndex);
			}

			if(_attr == null)
			{
				_attr = attribute as SRAttribute;
			}

			Event e = Event.current;
			var labelPosition = position;
			labelPosition.height = 20f;
			if(e.type == EventType.ContextClick && e.button == 1 && labelPosition.Contains(e.mousePosition) && !e.control)
			{
				GenericMenu context = new GenericMenu();

				if(_array != null)
				{
					context.AddItem(new GUIContent("Delete"), false, OnMenuItemClick, "Delete");
					context.AddItem(new GUIContent("Insert"), false, OnMenuItemClick, "Insert");
					//context.AddItem(new GUIContent("Add"), false, OnMenuItemClick, "Add");
					context.AddSeparator("");
				}
				if(_attr != null && _attr.Types != null)
				{
					context.AddItem(new GUIContent("Erase"), false, OnMenuItemClick, "Erase");
					context.AddSeparator("");
					for(int i = 0; i < _attr.Types.Length; ++i)
					{
						context.AddItem(new GUIContent(_attr.Types[i].Path), false, OnMenuItemClick, _attr.Types[i].Path);
					}
				}

				context.ShowAsContext();

				e.Use();
			}
		}

		EditorGUI.PropertyField(position, property, label, true);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property, label, true);
	}

	public void OnMenuItemClick(object userData)
	{
		string cmd = (string)userData;

		_element.serializedObject.UpdateIfRequiredOrScript();

		if(_array != null && _elementIndex >= 0 && _elementIndex < _array.arraySize)
		{
			_array.serializedObject.UpdateIfRequiredOrScript();

			if(cmd == "Delete")
			{
				Undo.RegisterCompleteObjectUndo(_array.serializedObject.targetObject, "Delete element at " + _elementIndex);
				Undo.FlushUndoRecordObjects();

				if(_element.objectReferenceValue != null)
					_element.objectReferenceValue = null;
				_element.managedReferenceValue = null;

				_array.DeleteArrayElementAtIndex(_elementIndex);
				_array.serializedObject.ApplyModifiedProperties();

				_element = null;
				return;
			}
			else if(cmd == "Insert")
			{
				Undo.RegisterCompleteObjectUndo(_array.serializedObject.targetObject, "Insert element at " + _elementIndex);
				Undo.FlushUndoRecordObjects();

				_array.InsertArrayElementAtIndex(_elementIndex);
				_array.serializedObject.ApplyModifiedProperties();

				var newElement = _array.GetArrayElementAtIndex(_elementIndex);
				if(newElement.objectReferenceValue != null)
					newElement.objectReferenceValue = null;
				newElement.managedReferenceValue = null;
				newElement.serializedObject.ApplyModifiedProperties();

				_element = null;
				return;
			}
			else if(cmd == "Add")
			{
				Undo.RegisterCompleteObjectUndo(_array.serializedObject.targetObject, "Add element at " + (_elementIndex + 1));
				Undo.FlushUndoRecordObjects();

				_array.InsertArrayElementAtIndex(_elementIndex + 1);
				_array.serializedObject.ApplyModifiedProperties();

				var newElement = _array.GetArrayElementAtIndex(_elementIndex + 1);
				if(newElement.objectReferenceValue != null)
					newElement.objectReferenceValue = null;
				newElement.managedReferenceValue = null;
				newElement.serializedObject.ApplyModifiedProperties();

				_element = null;
				return;
			}
		}

		if(cmd == "Erase")
		{
			Undo.RegisterCompleteObjectUndo(_element.serializedObject.targetObject, "Erase element");
			Undo.FlushUndoRecordObjects();

			if(_element.objectReferenceValue != null)
				_element.objectReferenceValue = null;
			_element.managedReferenceValue = null;
			_element.serializedObject.ApplyModifiedProperties();

			_element = null;
			return;
		}

		if(_attr == null)
			return;

		var typeInfo = _attr.TypeInfoByPath(cmd);
		if(typeInfo == null)
		{
			Debug.LogError("Type '" + cmd + "' not found.");
			return;
		}

		Undo.RegisterCompleteObjectUndo(_element.serializedObject.targetObject, "Create instance of " + typeInfo.Type);
		Undo.FlushUndoRecordObjects();

		var instance = Activator.CreateInstance(typeInfo.Type);
		_attr.OnCreate(instance);
		_element.managedReferenceValue = instance;
		_element.serializedObject.ApplyModifiedProperties();
	}

	private static SerializedProperty GetParentArray(SerializedProperty element, out int index)
	{
		index = -1;

		string propertyPath = element.propertyPath;
		if(!propertyPath.Contains(".Array.data[") || !propertyPath.EndsWith("]"))
			return null;

		string[] fullPathSplit = propertyPath.Split('.');

		string ending = fullPathSplit[fullPathSplit.Length - 1];
		index = 0;
		if(!int.TryParse(ending.Replace("data[", "").Replace("]", ""), out index))
			return null;

		string pathToArray = string.Empty;
		for(int i = 0; i < fullPathSplit.Length - 2; i++)
		{
			if(i < fullPathSplit.Length - 3)
			{
				pathToArray = string.Concat(pathToArray, fullPathSplit[i], ".");
			}
			else
			{
				pathToArray = string.Concat(pathToArray, fullPathSplit[i]);
			}
		}

		var targetObject = element.serializedObject.targetObject;
		SerializedObject serializedTargetObject = new SerializedObject(targetObject);

		return serializedTargetObject.FindProperty(pathToArray);
	}

	private static SerializedProperty GetArrayByElement(SerializedProperty element, out int index)
	{
		index = -1;

		var propertyPath = element.propertyPath;
		if(!propertyPath.EndsWith("]") || !propertyPath.Contains(".Array.data["))
			return null;

		var splitPath = propertyPath.Split('.');

		var last = splitPath[splitPath.Length - 1];
		if(!int.TryParse(last.Replace("data[", "").Replace("]", ""), out index))
			return null;

		string arrayPath = string.Empty;
		for(int i = 0; i < splitPath.Length - 2; i++)
		{
			arrayPath += splitPath[i];
			if(i < splitPath.Length - 3)
			{
				arrayPath += ".";
			}
		}

		SerializedObject serializedTargetObject = new SerializedObject(element.serializedObject.targetObject);

		return serializedTargetObject.FindProperty(arrayPath);
	}
}
