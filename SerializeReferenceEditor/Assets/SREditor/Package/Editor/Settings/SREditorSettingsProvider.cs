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
						EditorGUILayout.LabelField("Auto Processing Triggers", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(settings.FindProperty("_formerlySerializedTypeOnAssetImport"), new GUIContent("On Asset Import", "Process assets when they are imported"));
						EditorGUILayout.PropertyField(settings.FindProperty("_formerlySerializedTypeOnAssetSelect"), new GUIContent("On Project Asset Select", "Process only when selecting root assets in the Project window"));
						EditorGUILayout.PropertyField(settings.FindProperty("_formerlySerializedTypeOnSceneSave"), new GUIContent("On Scene Save", "Process scene roots when the scene is saved"));
						EditorGUILayout.PropertyField(settings.FindProperty("_processScenesOnOpen"), new GUIContent("On Scene Open", "Enqueue scene roots for processing when a scene is opened"));

						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Processing", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(settings.FindProperty("_processingBatchSize"), new GUIContent("Batch Size", "How many assets/objects to process per tick"));

						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Type Replacement & Cleanup", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(settings.FindProperty("_clearMissingReferencesIfNoReplacement"), new GUIContent("Clear Missing Managed References", "If no type replacement occurred, clear missing managed references on objects"));

						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Duplicate Handling", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(settings.FindProperty("_duplicateMode"), new GUIContent("Mode", "How to handle duplicate managed references"));

						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Change Detection Sources", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(settings.FindProperty("_doubleCleanOnEditorUpdate"), new GUIContent("On Editor Update", "Detect changes on selected objects during editor update"));
						EditorGUILayout.PropertyField(settings.FindProperty("_doubleCleanOnUndoRedo"), new GUIContent("On Undo/Redo", "Detect changes via Undo/Redo operations"));
						EditorGUILayout.PropertyField(settings.FindProperty("_doubleCleanOnAssetSave"), new GUIContent("On Asset Save", "Detect changes when assets are saved"));

						EditorGUILayout.Space();
						EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
						EditorGUILayout.PropertyField(settings.FindProperty("_missingTypesAssetFilter"), new GUIContent("Missing Types Filter", "Filter passed to AssetDatabase.FindAssets"));

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