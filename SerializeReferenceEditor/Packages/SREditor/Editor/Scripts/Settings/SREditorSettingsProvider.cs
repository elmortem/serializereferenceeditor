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
			"Project/SREditor",
			SettingsScope.Project,
			new HashSet<string>(new[] { "SREditor, SerializeReference" }))
		{
			label = "SREditor";
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
						var propertyField = settings.FindProperty("_showNameType");
						EditorGUILayout.PropertyField(propertyField);
						EditorGUILayout.PropertyField(settings.FindProperty("_nameSeparators"));

						if (!changeCheck.changed)
							return;

						settings.ApplyModifiedProperties();
						SREditorSettings.SaveSettings();
					}
				}
			}
		}
	}
}