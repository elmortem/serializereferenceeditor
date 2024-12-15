using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.Settings
{
	public class SREditorSettingsProvider : SettingsProvider
	{
		public SREditorSettingsProvider(
		) : base(
			"Project/Serialize Reference Editor",
			SettingsScope.Project,
			new HashSet<string>(new[] { "SREditor", "Serialize Reference Editor", "SerializeReference" }))
		{
			label = "Serialize Reference Editor";
			guiHandler = OnGuiHandler;
		}

		private void OnGuiHandler(string _)
		{
			var settings = new SerializedObject(SREditorSettings.GetOrCreateSettings());
			//using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(10f);
				using (new EditorGUILayout.VerticalScope())
				{
					using (var changeCheck = new EditorGUI.ChangeCheckScope())
					{
						EditorGUILayout.PropertyField(settings.FindProperty("_showNameType"));
						EditorGUILayout.PropertyField(settings.FindProperty("_nameSeparators"));
						
						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Formerly Serialized Type", EditorStyles.boldLabel);

						DrawCheckbox(settings, "_formerlySerializedTypeOnSceneSave");
						DrawCheckbox(settings, "_formerlySerializedTypeOnAssetSelect");
						DrawCheckbox(settings, "_formerlySerializedTypeOnAssetImport");
						
						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Double Clean", EditorStyles.boldLabel);
						
						DrawCheckbox(settings, "_doubleCleanOnEditorUpdate");
						DrawCheckbox(settings, "_doubleCleanOnUndoRedo");
						DrawCheckbox(settings, "_doubleCleanOnAssetSave");

						if (!changeCheck.changed)
							return;

						settings.ApplyModifiedProperties();
						SREditorSettings.SaveSettings();
					}
				}
			}
		}

		private static void DrawCheckbox(SerializedObject settings, string name)
		{
			var prop = settings.FindProperty(name);
			prop.boolValue = EditorGUI.ToggleLeft(
				EditorGUILayout.GetControlRect(),
				new GUIContent(prop.displayName, !string.IsNullOrEmpty(prop.tooltip) ? prop.tooltip : prop.displayName), 
				prop.boolValue
			);
		}
	}
}