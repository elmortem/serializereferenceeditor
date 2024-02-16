using System;
using System.Collections.Generic;
using System.Linq;
using SerializeReferenceEditor.Editor.Drawers;
using SerializeReferenceEditor.Editor.SRActions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SerializeReferenceEditor.Editor 
{
	[CustomPropertyDrawer(typeof(SRAttribute), false)]
	public class SRDrawer : PropertyDrawer
	{
		private static readonly SRCashTypeSearchTree _cash = new();
		private SRAttribute _srAttribute;
		private SerializedProperty _array;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			int index;
			if(_array == null)
				_array = GetParentArray(property, out index);
			else
				index = GetArrayIndex(property);

			_srAttribute ??= attribute as SRAttribute;
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
				ShowMenu(property);
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

		private void ShowMenu(SerializedProperty property)
		{
			if(_srAttribute.Types == null)
				_srAttribute.SetTypeByName(property.managedReferenceFieldTypename);

			var types = _srAttribute.Types;
			if (types == null)
			{
				Debug.LogError("Incorrect types");
				return;
			}

			var typeTreeFactory = _cash.GetTypeTreeFactory(types);
			var srActionFactory = new SRActionFactory(
				property,
				_array,
				_srAttribute);

			var searchWindow = SRTypesSearchWindowProvider.MakeTypesContainer(srActionFactory, typeTreeFactory);
			SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), searchWindow);
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
			var splitClassName = className.Split(' ');
			return Type.GetType(
				string.Format(
					"{0}, {1}",
					splitClassName[1],
					splitClassName[0]));
		}
	}
}