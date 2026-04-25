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
		private readonly SRDrawerOptions _options = new() { WithChild = true, ButtonTitle = true, DisableExpand = false };

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			_srAttribute ??= attribute as SRAttribute;
			Draw(position, property, label, _srAttribute?.Types);
		}

		private Type GetManagedReferenceFieldType(SerializedProperty property)
		{
			if (property == null || property.managedReferenceFieldTypename == null)
			{
				Debug.LogError($"Property '{property?.propertyPath}' has no managedReferenceFieldTypename");
				return null;
			}

			string[] typeSplit = property.managedReferenceFieldTypename.Split(new[] { ' ' }, 2);
			string typeAssembly = typeSplit[0];
			string typeClass = typeSplit[1];
			return Type.GetType(typeClass + ", " + typeAssembly);
		}

		public void Draw(Rect position, SerializedProperty property, GUIContent label,
			params Type[] types)
		{
			Draw(position, property, label, _options, types);
		}

		public void Draw(Rect position, SerializedProperty property, GUIContent label, SRDrawerOptions options, params Type[] types)
		{
			// Unity's UIElements ListView / reorderable list can delete an array element
			// mid-frame and still invoke this drawer with the now-stale SerializedProperty
			// for the removed slot. Any access to such a property throws
			// ObjectDisposedException ("...has disappeared!"). The Unity-side bug was
			// originally fixed in 2022.2.0a5 but re-regressed in 2022.3.x. Wrap the entire
			// draw — every property access is a potential trip wire — and silently bail.
			try
			{
				DrawCore(position, property, label, options, types);
			}
			catch (ObjectDisposedException)
			{
			}
		}

		private void DrawCore(Rect position, SerializedProperty property, GUIContent label, SRDrawerOptions options, Type[] types)
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

			var index = GetArrayIndex(property);

			string typeName = _nameService.GetTypeName(property.managedReferenceFullTypename);
			var buttonTitle = typeName + (index >= 0 ? ("[" + index + "]") : "");
			var buttonContent = new GUIContent(options.ButtonTitle ? buttonTitle : string.Empty);

			float buttonWidth = 10f + GUI.skin.button.CalcSize(buttonContent).x;
			var lastIsExpanded = property.isExpanded;
			property.isExpanded = false;
			float buttonHeight = EditorGUI.GetPropertyHeight(property, label, false);
			property.isExpanded = lastIsExpanded;

			var bgColor = GUI.backgroundColor;
			GUI.backgroundColor = Color.green;
			var buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, buttonHeight);

			if (EditorGUI.DropdownButton(buttonRect, buttonContent, FocusType.Passive))
			{
				ShowTypeSelectionMenu(property, typeInfos);
				Event.current.Use();
			}
			GUI.backgroundColor = bgColor;

			var propertyRect = position;

			if (options.DisableExpand)
				EditorGUI.LabelField(propertyRect, label);
			else
				EditorGUI.PropertyField(propertyRect, property, label, options.WithChild);
		}
		
		public float GetButtonWidth(SerializedProperty property, SRDrawerOptions options)
		{
			var index = GetArrayIndex(property);

			string typeName = _nameService.GetTypeName(property.managedReferenceFullTypename);
			var buttonTitle = typeName + (index >= 0 ? ("[" + index + "]") : "");
			var buttonContent = new GUIContent(options.ButtonTitle ? buttonTitle : string.Empty);

			return 10f + GUI.skin.button.CalcSize(buttonContent).x;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return GetPropertyHeight(property, label, true);
		}
		
		public float GetPropertyHeight(SerializedProperty property, GUIContent label, bool includeChild)
		{
			return EditorGUI.GetPropertyHeight(property, label, includeChild);
		}

		private void ShowTypeSelectionMenu(SerializedProperty property, TypeInfo[] typeInfos)
		{
			if (typeInfos == null)
			{
				Debug.LogError("Type infos array cannot be null");
				return;
			}

			var typeTreeFactory = _cash.GetTypeTreeFactory(typeInfos);
			var srActionFactory = new SRActionFactory(property.Copy(), typeInfos);

			var searchWindow = SRTypesSearchWindowProvider.MakeTypesContainer(srActionFactory, typeTreeFactory);
			SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), searchWindow);
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