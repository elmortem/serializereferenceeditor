using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.ClassReplacer
{
	[InitializeOnLoad]
	public class SRTypeUpgrader : AssetPostprocessor
	{
		private static HashSet<string> _processedAssets = new();

		static SRTypeUpgrader()
		{
			_processedAssets.Clear();

			EditorSceneManager.sceneSaving -= OnSceneSaving;
			EditorSceneManager.sceneSaving += OnSceneSaving;

			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;

			OnSelectionChanged();
		}

		private static void OnSelectionChanged()
		{
			if (Selection.objects != null && Selection.objects.Length > 0)
			{
				foreach (var obj in Selection.objects)
				{
					ProcessObject(obj);
				}
			}
		}

		private static void ProcessObject(Object obj)
		{
			if (obj == null) return;

			var assetPath = AssetDatabase.GetAssetPath(obj);
			if (string.IsNullOrEmpty(assetPath)) return;

			if (PrefabUtility.IsPartOfPrefabAsset(obj))
			{
				if (_processedAssets.Contains(assetPath)) return;
				_processedAssets.Add(assetPath);
			}

			bool modified = false;
			foreach (var (oldAssembly, oldType, newType) in SRTypeCache.GetAllReplacements())
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
			}
		}

		private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
		{
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

		private static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			foreach (var assetPath in importedAssets)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
				if (asset != null)
				{
					ProcessObject(asset);
				}
			}
		}
	}
}