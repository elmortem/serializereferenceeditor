using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SRAction
{
	public SerializedProperty Property;
	public string Command;

	public SRAction(SerializedProperty p, string c)
	{
		Property = p;
		Command = c;
	}
}

[CustomPropertyDrawer(typeof(SRAttribute), false)]
public class SRDrawer : PropertyDrawer
{
	private SRAttribute _attr;
	private SerializedProperty _array;
	private Dictionary<SerializedProperty, int> _elementIndexes = new Dictionary<SerializedProperty, int>();

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		int index;
		if(_array == null)
		{
			_array = GetParentArray(property, out index);
		}
		else
		{
			index = GetArrayIndex(property);
		}
		_elementIndexes[property] = index;

		if(_attr == null)
		{
			_attr = attribute as SRAttribute;
		}
		
		var typeName = GetTypeName(property.managedReferenceFullTypename);
		var typeNameContent = new GUIContent(typeName + (_array != null ? ("[" + index + "]") : ""));

		float buttonWidth = 10f + GUI.skin.button.CalcSize(typeNameContent).x;
		float buttonHeight = EditorGUI.GetPropertyHeight(property, label, false);

		EditorGUI.BeginChangeCheck();

		var bgColor = GUI.backgroundColor;
		GUI.backgroundColor = Color.green;
		var buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, buttonHeight);
		if(EditorGUI.DropdownButton(buttonRect, typeNameContent, FocusType.Passive))
		{
			ShowMenu(property, true);
			Event.current.Use();
		}
		GUI.backgroundColor = bgColor;
		
		var propertyRect = position;
		EditorGUI.PropertyField(propertyRect, property, label, true);

		if(EditorGUI.EndChangeCheck() && _attr != null)
		{
			//TODO _attr.OnChange(_element.managedReferenceValue);
		}
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUI.GetPropertyHeight(property, label, true);
	}

	private void ShowMenu(SerializedProperty property, bool applyArray)
	{
		GenericMenu context = new GenericMenu();

		if(_array != null && applyArray)
		{
			int index = _elementIndexes[property];
			context.AddItem(new GUIContent("Delete"), false, OnMenuItemClick, new SRAction(property, "Delete"));
			context.AddItem(new GUIContent("Insert"), false, OnMenuItemClick, new SRAction(property, "Insert"));
			context.AddItem(new GUIContent("Add"), false, OnMenuItemClick, new SRAction(property, "Add"));
			context.AddSeparator("");
		}

		if(_attr.Types == null)
			_attr.SetTypeByName(property.managedReferenceFieldTypename);

		var types = _attr.Types;
		if(types != null)
		{
			context.AddItem(new GUIContent("Erase"), false, OnMenuItemClick, new SRAction(property, "Erase"));
			context.AddSeparator("");
			for(int i = 0; i < types.Length; ++i)
			{
				context.AddItem(new GUIContent(types[i].Path), false, OnMenuItemClick, new SRAction(property, types[i].Path));
			}
		}

		context.ShowAsContext();
	}

	public void OnMenuItemClick(object userData)
	{
		var action = (SRAction)userData;
		var cmd = action.Command;
		var element = action.Property;
		var index = -1;
		if(_array != null)
			index = _elementIndexes[element];

		element.serializedObject.UpdateIfRequiredOrScript();

		if(_array != null && index >= 0 && index < _array.arraySize)
		{
			_array.serializedObject.UpdateIfRequiredOrScript();

			if(cmd == "Delete")
			{
				Undo.RegisterCompleteObjectUndo(_array.serializedObject.targetObject, "Delete element at " + index);
				Undo.FlushUndoRecordObjects();

				element.managedReferenceValue = null;

				_array.DeleteArrayElementAtIndex(index);
				_array.serializedObject.ApplyModifiedProperties();

				_array = null;

				return;
			}
			else if(cmd == "Insert")
			{
				Undo.RegisterCompleteObjectUndo(_array.serializedObject.targetObject, "Insert element at " + index);
				Undo.FlushUndoRecordObjects();

				_array.InsertArrayElementAtIndex(index);
				_array.serializedObject.ApplyModifiedProperties();

				var newElement = _array.GetArrayElementAtIndex(index);
				newElement.managedReferenceValue = null;
				newElement.serializedObject.ApplyModifiedProperties();

				return;
			}
			else if(cmd == "Add")
			{
				Undo.RegisterCompleteObjectUndo(_array.serializedObject.targetObject, "Add element at " + (index + 1));
				Undo.FlushUndoRecordObjects();

				_array.InsertArrayElementAtIndex(index + 1);
				_array.serializedObject.ApplyModifiedProperties();

				var newElement = _array.GetArrayElementAtIndex(index + 1);
				newElement.managedReferenceValue = null;
				newElement.serializedObject.ApplyModifiedProperties();

				return;
			}
		}

		if(cmd == "Erase")
		{
			Undo.RegisterCompleteObjectUndo(element.serializedObject.targetObject, "Erase element");
			Undo.FlushUndoRecordObjects();

			element.managedReferenceValue = null;
			element.serializedObject.ApplyModifiedProperties();

			return;
		}

		var typeInfo = _attr.TypeInfoByPath(cmd);
		if(typeInfo == null)
		{
			Debug.LogError("Type '" + cmd + "' not found.");
			return;
		}

		Undo.RegisterCompleteObjectUndo(element.serializedObject.targetObject, "Create instance of " + typeInfo.Type);
		Undo.FlushUndoRecordObjects();

		var instance = Activator.CreateInstance(typeInfo.Type);
		_attr.OnCreate(instance);

		element.managedReferenceValue = instance;
		element.serializedObject.ApplyModifiedProperties();
	}

	private static SerializedProperty GetParentArray(SerializedProperty element, out int index)
	{
		index = GetArrayIndex(element);
		if(index < 0)
			return null;

		string propertyPath = element.propertyPath;

		string[] fullPathSplit = propertyPath.Split('.');

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

	private static int GetArrayIndex(SerializedProperty element)
	{
		string propertyPath = element.propertyPath;
		if(!propertyPath.Contains(".Array.data[") || !propertyPath.EndsWith("]"))
			return -1;

		int start = propertyPath.LastIndexOf("[");
		var str = propertyPath.Substring(start + 1, propertyPath.Length - start - 2);
		int index;
		int.TryParse(str, out index);
		return index;
	}

	private static string GetTypeName(string typeName)
	{
		if(string.IsNullOrEmpty(typeName))
			return "(empty)";

		var index = typeName.LastIndexOf(' ');
		if(index >= 0)
			return typeName.Substring(index + 1);

		index = typeName.LastIndexOf('.');
		if(index >= 0)
			return typeName.Substring(index + 1);

		return typeName;
	}
}
