using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.Settings
{
	public class SREditorSettingsProvider : SettingsProvider
	{
		public const string ProjectSettingsPath ="Project/Serialize Reference Editor";
		
		public SREditorSettingsProvider(
		) : base(
			ProjectSettingsPath,
			SettingsScope.Project,
			new HashSet<string>(new[] { "SREditor", "Serialize Reference Editor", "SerializeReference" }))
		{
			label = "Serialize Reference Editor";
			guiHandler = OnGuiHandler;
		}

		private void OnGuiHandler(string _)
		{
			var settings = new SerializedObject(SREditorSettings.GetOrCreateSettings());
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(10f);
				using (new EditorGUILayout.VerticalScope())
				{
					using (var changeCheck = new EditorGUI.ChangeCheckScope())
					{
						EditorGUILayout.PropertyField(settings.FindProperty(nameof(SREditorSettings._showNameType)));
						EditorGUILayout.PropertyField(settings.FindProperty(nameof(SREditorSettings._nameSeparators)));
						
						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Formerly Serialized Type", EditorStyles.boldLabel);

						DrawCheckbox(settings, nameof(SREditorSettings._formerlySerializedTypeOnSceneSave));
						DrawCheckbox(settings, nameof(SREditorSettings._formerlySerializedTypeOnAssetSelect));
						DrawCheckbox(settings, nameof(SREditorSettings._formerlySerializedTypeOnAssetImport));
						
						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Double Clean", EditorStyles.boldLabel);
						
						DrawCheckbox(settings, nameof(SREditorSettings._doubleCleanOnEditorUpdate));
						DrawCheckbox(settings, nameof(SREditorSettings._doubleCleanOnUndoRedo));
						DrawCheckbox(settings, nameof(SREditorSettings._doubleCleanOnAssetSave));

						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Duplicate Handling", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(settings.FindProperty(nameof(SREditorSettings._duplicateMode)));

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