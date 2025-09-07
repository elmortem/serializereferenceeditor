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
		internal bool _clearMissingReferencesIfNoReplacement = false;
		[SerializeField]
		internal bool _doubleCleanOnEditorUpdate = true;
		[SerializeField]
		internal bool _doubleCleanOnUndoRedo = true;
		[SerializeField]
		internal bool _doubleCleanOnAssetSave = true;
		[SerializeField]
		internal SRDuplicateMode _duplicateMode = SRDuplicateMode.Default;
		[SerializeField]
		internal string _missingTypesAssetFilter = "t:Object";
		[SerializeField]
		internal int _processingBatchSize = 2;
		[SerializeField]
		internal bool _processScenesOnOpen = false;
		
		public ShowNameType ShowNameType => _showNameType;
		public char[] NameSeparators => _nameSeparators;
		public bool FormerlySerializedTypeOnSceneSave => _formerlySerializedTypeOnSceneSave;
		public bool FormerlySerializedTypeOnAssetSelect => _formerlySerializedTypeOnAssetSelect;
		public bool FormerlySerializedTypeOnAssetImport => _formerlySerializedTypeOnAssetImport;
		public bool ClearMissingReferencesIfNoReplacement => _clearMissingReferencesIfNoReplacement;
		public bool DoubleCleanOnEditorUpdate => _doubleCleanOnEditorUpdate;
		public bool DoubleCleanOnUndoRedo => _doubleCleanOnUndoRedo;
		public bool DoubleCleanOnAssetSave => _doubleCleanOnAssetSave;
		public SRDuplicateMode DuplicateMode => _duplicateMode;
		public string MissingTypesAssetFilter => _missingTypesAssetFilter;
		public int ProcessingBatchSize => _processingBatchSize > 0 ? _processingBatchSize : 1;
		public bool ProcessScenesOnOpen => _processScenesOnOpen;
		
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