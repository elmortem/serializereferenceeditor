using System;
using SerializeReferenceEditor.Editor.Drawers;
using SerializeReferenceEditor.Editor.Services;
using SerializeReferenceEditor.Editor.SRActions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SerializeReferenceEditor.Editor 
{
	[CustomPropertyDrawer(typeof(SRAttribute), false)]
	public class SRDrawer : PropertyDrawer
	{
		private readonly NameService _nameService = new();
		private static readonly SRCashTypeSearchTree _cash = new();
		private SRAttribute _srAttribute;
		private SerializedProperty _array;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			_srAttribute ??= attribute as SRAttribute;
			Draw(position, property, label, _srAttribute?.Types);
		}

		private Type GetManagedReferenceFieldType(SerializedProperty property)
		{
			string[] typeSplit = property.managedReferenceFieldTypename.Split(char.Parse(" "));
			string typeAssembly = typeSplit[0];
			string typeClass = typeSplit[1];
			return Type.GetType(typeClass + ", " + typeAssembly);
		}

		public void Draw(Rect position, SerializedProperty property, GUIContent label, params Type[] types)
		{
			TypeInfo[] typeInfos;
			if (types == null || types.Length == 0)
			{
				var managedReferenceFieldType = GetManagedReferenceFieldType(property);
				typeInfos = SRTypeCache.GetTypeInfos(managedReferenceFieldType);
			}
			else if (types.Length == 1)
			{
				typeInfos = SRTypeCache.GetTypeInfos(types[0]);
			}
			else
			{
				typeInfos = SRTypeCache.GetTypeInfos(types);
			}

			int index;
			if (_array == null)
			{
				_array = GetParentArray(property, out index);
			}
			else
			{
				index = GetArrayIndex(property);
			}

			string typeName = _nameService.GetTypeName(property.managedReferenceFullTypename);
			var typeNameContent = new GUIContent(typeName + (_array != null ? ("[" + index + "]") : ""));

			float buttonWidth = 10f + GUI.skin.button.CalcSize(typeNameContent).x;
			float buttonHeight = EditorGUI.GetPropertyHeight(property, label, false);

			var bgColor = GUI.backgroundColor;
			GUI.backgroundColor = Color.green;
			var buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, buttonHeight);
			
			if (EditorGUI.DropdownButton(buttonRect, typeNameContent, FocusType.Passive))
			{
				ShowTypeSelectionMenu(property, typeInfos);
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

		private void ShowTypeSelectionMenu(SerializedProperty property, TypeInfo[] typeInfos)
		{
			if (typeInfos == null)
			{
				Debug.LogError("Type infos array cannot be null");
				return;
			}

			var typeTreeFactory = _cash.GetTypeTreeFactory(typeInfos);
			var srActionFactory = new SRActionFactory(property, _array, typeInfos);

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
	}
}