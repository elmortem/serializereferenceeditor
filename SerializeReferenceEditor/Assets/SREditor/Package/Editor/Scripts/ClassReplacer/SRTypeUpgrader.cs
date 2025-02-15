using System.Collections.Generic;
using SerializeReferenceEditor.Editor.Settings;
using SerializeReferenceEditor.Services;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.ClassReplacer
{
	[InitializeOnLoad]
	public class SRTypeUpgrader : AssetPostprocessor
	{
		private static HashSet<string> _processedAssets = new HashSet<string>();
		private static HashSet<int> _processingObjects = new HashSet<int>();

		static SRTypeUpgrader()
		{
			_processedAssets.Clear();
			_processingObjects.Clear();

			EditorSceneManager.sceneSaving -= OnSceneSaving;
			EditorSceneManager.sceneSaving += OnSceneSaving;

			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;

			OnSelectionChanged();
		}
		
		private static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			if (!SREditorSettings.GetOrCreateSettings()?.FormerlySerializedTypeOnAssetImport??false)
				return;
			
			foreach (var assetPath in importedAssets)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
				if (asset != null)
				{
					ProcessObject(asset);
				}
			}
		}
		
		private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
		{
			if (!SREditorSettings.GetOrCreateSettings()?.FormerlySerializedTypeOnSceneSave??false)
				return;
			
			foreach (var rootObj in scene.GetRootGameObjects())
			{
				foreach (var component in rootObj.GetComponentsInChildren<Component>(true))
				{
					if (component != null)
					{
						ProcessObject(component);
					}
				}
			}
		}
		
		private static void OnSelectionChanged()
		{
			if (!SREditorSettings.GetOrCreateSettings()?.FormerlySerializedTypeOnAssetSelect??false)
				return;
			
			if (Selection.objects != null && Selection.objects.Length > 0)
			{
				foreach (var obj in Selection.objects)
				{
                    if (_processingObjects.Contains(obj.GetInstanceID()))
                    	continue;

					ProcessObject(obj);
				}
			}
		}

		private static void ProcessObject(Object obj)
		{
			if (obj == null) 
				return;

			int instanceId = obj.GetInstanceID();
			if (!_processingObjects.Add(instanceId)) 
				return;

			try
			{
				var assetPath = AssetDatabase.GetAssetPath(obj);
				if (string.IsNullOrEmpty(assetPath)) 
					return;

				if (PrefabUtility.IsPartOfPrefabAsset(obj))
				{
					if (!_processedAssets.Add(assetPath)) 
						return;
				}

				bool modified = false;
				foreach (var (oldAssembly, oldType, newType) in SRFormerlyTypeCache.GetAllReplacements())
				{
					var oldTypePattern = string.IsNullOrEmpty(oldAssembly) ? oldType : $"{oldAssembly}, {oldType}";
					var newAssembly = newType.Assembly.GetName().Name;
					var newTypePattern = string.IsNullOrEmpty(newAssembly)
						? newType.FullName
						: $"{newAssembly}, {newType.FullName}";

					if (TypeReplacer.ReplaceTypeInFile(assetPath, oldTypePattern, newTypePattern))
					{
						modified = true;
					}
				}

				if (modified)
				{
					AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
					EditorUtility.SetDirty(obj);

					EditorApplication.delayCall += () =>
					{
						if (Selection.activeObject == obj)
						{
							Selection.activeObject = null;
							EditorApplication.delayCall += () =>
							{
								Selection.activeObject = obj;
								_processingObjects.Remove(instanceId);
								_processedAssets.Clear();
							};
						}
						else
						{
							_processingObjects.Remove(instanceId);
							_processedAssets.Clear();
						}
					};
				}
				else
				{
					_processingObjects.Remove(instanceId);
				}
			}
			catch
			{
				_processingObjects.Remove(instanceId);
				throw;
			}
		}
	}
}