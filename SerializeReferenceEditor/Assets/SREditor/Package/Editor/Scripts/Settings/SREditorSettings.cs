using System.IO;
using UnityEditor;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.Settings
{
	[CreateAssetMenu(fileName = "SREditorSettings",
		menuName = "SREditor/Settings",
		order = 0)]
	public class SREditorSettings : ScriptableObject
	{
		private const string SettingsPath = "ProjectSettings/SREditorSettings.json";
		private static SREditorSettings _instance;

		[SerializeField]
		private ShowNameType _showNameType = ShowNameType.OnlyCurrentType;
		[SerializeField]
		private char[] _nameSeparators = { '/', '.' };
		public ShowNameType ShowNameType => _showNameType;
		public char[] NameSeparators => _nameSeparators;

		public static SREditorSettings GetOrCreateSettings()
		{
			if (_instance != null) 
				return _instance;

			_instance = CreateInstance<SREditorSettings>();
			if (File.Exists(SettingsPath))
			{
				JsonUtility.FromJsonOverwrite(File.ReadAllText(SettingsPath), _instance);
			}

			return _instance;
		}

		public static void SaveSettings() 
			=> File.WriteAllText(SettingsPath, JsonUtility.ToJson(_instance, true));

		[SettingsProvider]
		internal static SettingsProvider CreateSettingsProvider() 
			=> new SREditorSettingsProvider();
	}
}