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
		internal ShowNameType _showNameType = ShowNameType.OnlyCurrentType;
		[SerializeField]
		internal char[] _nameSeparators = { '/', '.' };
		[SerializeField]
		internal bool _formerlySerializedTypeOnSceneSave = true;
		[SerializeField]
		internal bool _formerlySerializedTypeOnAssetSelect = true;
		[SerializeField]
		internal bool _formerlySerializedTypeOnAssetImport = true;
		[SerializeField]
		internal bool _doubleCleanOnEditorUpdate = true;
		[SerializeField]
		internal bool _doubleCleanOnUndoRedo = true;
		[SerializeField]
		internal bool _doubleCleanOnAssetSave = true;
		[SerializeField]
		internal SRDuplicateMode _duplicateMode = SRDuplicateMode.Default;
		
		public ShowNameType ShowNameType => _showNameType;
		public char[] NameSeparators => _nameSeparators;
		public bool FormerlySerializedTypeOnSceneSave => _formerlySerializedTypeOnSceneSave;
		public bool FormerlySerializedTypeOnAssetSelect => _formerlySerializedTypeOnAssetSelect;
		public bool FormerlySerializedTypeOnAssetImport => _formerlySerializedTypeOnAssetImport;
		public bool DoubleCleanOnEditorUpdate => _doubleCleanOnEditorUpdate;
		public bool DoubleCleanOnUndoRedo => _doubleCleanOnUndoRedo;
		public bool DoubleCleanOnAssetSave => _doubleCleanOnAssetSave;
		public SRDuplicateMode DuplicateMode => _duplicateMode;
		
		[InitializeOnLoadMethod]
		static void Initialize()
		{
			var settings = GetOrCreateSettings();
			AssetChangeDetector.Initialize(settings.DoubleCleanOnEditorUpdate, settings.DoubleCleanOnUndoRedo,
				settings.DoubleCleanOnAssetSave);
		}

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
		{
			File.WriteAllText(SettingsPath, JsonUtility.ToJson(_instance, true));
			Initialize();
		}

		[SettingsProvider]
		internal static SettingsProvider CreateSettingsProvider() 
			=> new SREditorSettingsProvider();
		
		[MenuItem("Tools/SREditor/Settings")]
		static void OpenProjectSettings()
		{
			SettingsService.OpenProjectSettings(SREditorSettingsProvider.ProjectSettingsPath);
		}
	}
}