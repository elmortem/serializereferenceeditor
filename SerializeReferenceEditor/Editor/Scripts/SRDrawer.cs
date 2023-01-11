using System;
using System.Collections.Generic;
using System.Linq;
using SerializeReferenceEditor.Scripts;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.Scripts 
{
	[CustomPropertyDrawer(typeof(SRAttribute), false)]
	public class SRDrawer : PropertyDrawer
	{
		private SRAttribute _srAttribute;
		private SerializedProperty _array;
		private Dictionary<SerializedProperty, int> _elementIndexes = new Dictionary<SerializedProperty, int>();
	
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			int index;
			if(_array == null)
				_array = GetParentArray(property, out index);
			else
				index = GetArrayIndex(property);
			_elementIndexes[property] = index;

			_srAttribute ??= attribute as SRAttribute;
			var typeName = GetTypeName(property.managedReferenceFullTypename);
			var typeNameContent = new GUIContent(typeName + (_array != null ? ("[" + index + "]") : ""));

			float buttonWidth = 10f + GUI.skin.button.CalcSize(typeNameContent).x;
			float buttonHeight = EditorGUI.GetPropertyHeight(property, label, false);

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
				context.AddItem(new GUIContent("Delete"), false, OnMenuItemClick, new SRAction(property, "Delete"));
				context.AddItem(new GUIContent("Insert"), false, OnMenuItemClick, new SRAction(property, "Insert"));
				context.AddItem(new GUIContent("Add"), false, OnMenuItemClick, new SRAction(property, "Add"));
				context.AddSeparator("");
			}

			if(_srAttribute.Types == null)
				_srAttribute.SetTypeByName(property.managedReferenceFieldTypename);

			var types = _srAttribute.Types;
			if(types != null)
			{
				context.AddItem(new GUIContent("Erase"), false, OnMenuItemClick, new SRAction(property, "Erase"));
				context.AddSeparator("");
				for(int i = 0; i < types.Length; ++i)
				{
					var typeName = types[i].Path;
					context.AddItem(new GUIContent(typeName), false, OnMenuItemClick, new SRAction(property, types[i].Path));
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

				if(cmd == "Insert")
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

				if(cmd == "Add")
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

			var typeInfo = _srAttribute.TypeInfoByPath(cmd);
			if(typeInfo == null)
			{
				Debug.LogErrorFormat("Type '{0}' not found.", cmd);
				return;
			}

			Undo.RegisterCompleteObjectUndo(element.serializedObject.targetObject, "Create instance of " + typeInfo.Type);
			Undo.FlushUndoRecordObjects();

			var instance = Activator.CreateInstance(typeInfo.Type);
			_srAttribute.OnCreate(instance);

			element.managedReferenceValue = instance;
			element.serializedObject.ApplyModifiedProperties();
		}

		private static SerializedProperty GetParentArray(SerializedProperty element, out int index)
		{
			index = GetArrayIndex(element);
			if(index < 0)
				return null;

			string[] fullPathSplit = element.propertyPath.Split('.');

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
			var propertyPath = element.propertyPath;
			if(!propertyPath.Contains(".Array.data[") || !propertyPath.EndsWith("]"))
				return -1;

			var start = propertyPath.LastIndexOf("[", StringComparison.Ordinal);
			var str = propertyPath.Substring(start + 1, propertyPath.Length - start - 2);
			int.TryParse(str, out var index);
			return index;
		}

		private static string GetTypeName(string typeName)
		{
			if(string.IsNullOrEmpty(typeName))
				return "(empty)";

			if (TypeByName(typeName)?
				    .GetCustomAttributes(typeof(SRNameAttribute), false)
				    .FirstOrDefault()
			    is SRNameAttribute nameAttr)
				return nameAttr.Name;
		
			var index = typeName.LastIndexOf(' ');
			if(index >= 0)
				return typeName.Substring(index + 1);

			index = typeName.LastIndexOf('.');
			if(index >= 0)
				return typeName.Substring(index + 1);

			return typeName;
		}
	
		private static Type TypeByName(string className)
		{
			var splitedClassName = className.Split(' ');
			return Type.GetType(
				string.Format(
					"{1}, {0}",
					splitedClassName[0],
					splitedClassName[1]));
		}
	}
}