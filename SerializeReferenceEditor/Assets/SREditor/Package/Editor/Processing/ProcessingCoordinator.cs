using System;
using System.Collections.Generic;
using SerializeReferenceEditor.Editor.Processing.DoubleClean;
using SerializeReferenceEditor.Editor.Processing.TypeReplace;
using SerializeReferenceEditor.Editor.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.Processing
{
	public class ProcessingCoordinator : AssetPostprocessor
	{
		private static readonly HashSet<string> PendingAssetPaths = new();
		private static readonly HashSet<Object> PendingSceneObjects = new();
		private static readonly HashSet<string> InFlightImports = new();

		private static bool _processingScheduled;
		private static bool _isProcessing;

		[InitializeOnLoadMethod]
		private static void Initialize()
		{
			Selection.selectionChanged -= OnSelectionChanged;
			Selection.selectionChanged += OnSelectionChanged;

			EditorSceneManager.sceneSaving -= OnSceneSaving;
			EditorSceneManager.sceneSaving += OnSceneSaving;

			EditorSceneManager.sceneOpened -= OnSceneOpened;
			EditorSceneManager.sceneOpened += OnSceneOpened;

			AssetChangeDetector.ChangeEvent -= OnAssetChanged;
			AssetChangeDetector.ChangeEvent += OnAssetChanged;
		}

		private static SREditorSettings Settings => SREditorSettings.GetOrCreateSettings();

		private static bool IsProcessableAssetPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return false;
			return path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase)
				   || path.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase)
				   || path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase);
		}

		static void OnPostprocessAllAssets(
			string[] importedAssets,
			string[] deletedAssets,
			string[] movedAssets,
			string[] movedFromAssetPaths)
		{
			if (deletedAssets != null)
			{
				foreach (var p in deletedAssets)
				{
					if (!IsProcessableAssetPath(p)) 
						continue;
					
					PendingAssetPaths.Remove(p);
					InFlightImports.Remove(p);
				}
			}

			if (movedFromAssetPaths != null)
			{
				foreach (var p in movedFromAssetPaths)
				{
					if (!IsProcessableAssetPath(p)) 
						continue;
					
					PendingAssetPaths.Remove(p);
					InFlightImports.Remove(p);
				}
			}

			if (!Settings.FormerlySerializedTypeOnAssetImport)
				return;

			foreach (var path in importedAssets)
			{
				if (string.IsNullOrEmpty(path))
					continue;
				if (!IsProcessableAssetPath(path))
					continue;
				
				if (InFlightImports.Remove(path))
					continue;

				EnqueueAssetPath(path);
			}

			// Treat moved assets as newly imported at their new path
			foreach (var path in movedAssets)
			{
				if (string.IsNullOrEmpty(path))
					continue;
				if (!IsProcessableAssetPath(path))
					continue;

				if (InFlightImports.Remove(path))
					continue;

				EnqueueAssetPath(path);
			}
		}

		private static void OnAssetChanged(Object changedObject)
		{
			if (_isProcessing)
				return;

			if (changedObject == null)
				return;

			var assetPath = AssetDatabase.GetAssetPath(changedObject);
			if (!IsProcessableAssetPath(assetPath))
				return;
			
			EnqueueAssetPath(assetPath);
		}

		private static void OnSelectionChanged()
		{
			if (!Settings.FormerlySerializedTypeOnAssetSelect)
				return;

			foreach (var obj in Selection.objects)
			{
				if (obj == null)
					continue;

				var path = AssetDatabase.GetAssetPath(obj);
				if (!IsProcessableAssetPath(path))
					continue; // ignore assets we don't process

				if (!AssetDatabase.IsMainAsset(obj))
					continue; // ignore sub-assets or non-root selections

				EnqueueAssetPath(path);
			}
		}

		private static void OnSceneSaving(Scene scene, string path)
		{
			if (!Settings.FormerlySerializedTypeOnSceneSave)
				return;

			var roots = scene.GetRootGameObjects();
			bool anyChanged = false;
			foreach (var go in roots)
			{
				bool modified = TypeReplacer.TryUpgradeAsset(string.Empty, go, out bool clearedMissing);
				modified |= SRDuplicateCleaner.TryCleanupObject(go, Settings.DuplicateMode);
				anyChanged |= modified | clearedMissing;
			}

			if (anyChanged)
			{
				EditorSceneManager.MarkSceneDirty(scene);
			}
		}

		private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
		{
			if (!Settings.ProcessScenesOnOpen)
				return;

			var roots = scene.GetRootGameObjects();
			foreach (var go in roots)
			{
				if (go == null) 
					continue;
				
				EnqueueSceneObject(go);
			}
		}

		private static void EnqueueAssetPath(string path)
		{
			if (PendingAssetPaths.Add(path))
				ScheduleProcessing();
		}

		private static void EnqueueSceneObject(Object obj)
		{
			if (obj == null)
				return;
			if (PendingSceneObjects.Add(obj))
				ScheduleProcessing();
		}

		private static void ScheduleProcessing()
		{
			if (_processingScheduled)
				return;
			_processingScheduled = true;
			EditorApplication.delayCall += ProcessBatch;
		}

		private static void ProcessBatch()
		{
			_processingScheduled = false;
			if (_isProcessing)
				return;
			_isProcessing = true;

			try
			{
				int batchSize = System.Math.Max(1, Settings.ProcessingBatchSize);

				// Take a batch of asset paths
				var paths = new List<string>(batchSize);
				using (var enumerator = PendingAssetPaths.GetEnumerator())
				{
					while (paths.Count < batchSize && enumerator.MoveNext())
					{
						var p = enumerator.Current;
						if (!string.IsNullOrEmpty(p))
							paths.Add(p);
					}
				}
				// Remove taken items from the pending set
				foreach (var p in paths)
					PendingAssetPaths.Remove(p);

				// Take a batch of scene objects (remaining budget)
				int remainingBudget = System.Math.Max(0, batchSize - paths.Count);
				var sceneObjects = new List<Object>(remainingBudget);
				using (var enumerator = PendingSceneObjects.GetEnumerator())
				{
					while (sceneObjects.Count < remainingBudget && enumerator.MoveNext())
					{
						var o = enumerator.Current;
						if (o != null)
							sceneObjects.Add(o);
					}
				}
				foreach (var o in sceneObjects)
					PendingSceneObjects.Remove(o);

				var reimportPaths = new HashSet<string>();
				var dirtyAssets = new HashSet<Object>();

				foreach (var path in paths)
				{
					// 1) Try text-based replacement without loading asset
					bool modified = TypeReplacer.TryUpgradeAsset(path, null, out bool _);

					bool clearedMissing = false;
					bool cleaned = false;
					Object loadedAsset = null;

					// 2) Only if nothing changed by text pass, optionally load and process object
					bool wantClearMissing = Settings.ClearMissingReferencesIfNoReplacement;
					bool wantDuplicateClean = Settings.DuplicateMode != SerializeReferenceEditor.Editor.Settings.SRDuplicateMode.Default;
					if (!modified && (wantClearMissing || wantDuplicateClean))
					{
						loadedAsset = AssetDatabase.LoadAssetAtPath<Object>(path);
						if (loadedAsset != null)
						{
							if (wantDuplicateClean)
							{
								cleaned = SRDuplicateCleaner.TryCleanupObject(loadedAsset, Settings.DuplicateMode);
							}

							if (wantClearMissing)
							{
								// Use empty assetPath to only clear missing refs without re-running text replacement
								bool changedByClear = TypeReplacer.TryUpgradeAsset(string.Empty, loadedAsset, out bool cleared);
								clearedMissing = cleared;
								modified |= changedByClear;
							}
						}
					}

					if (modified && !string.IsNullOrEmpty(path))
					{
						reimportPaths.Add(path);
					}

					if (loadedAsset != null && (clearedMissing || cleaned))
					{
						EditorUtility.SetDirty(loadedAsset);
						dirtyAssets.Add(loadedAsset);
					}
				}

				foreach (var obj in sceneObjects)
				{
					if (obj == null) continue;

					string scenePath = string.Empty;
					if (obj is Component c && c.gameObject.scene.IsValid())
						scenePath = c.gameObject.scene.path;
					else if (obj is GameObject go && go.scene.IsValid())
						scenePath = go.scene.path;

					bool modified = TypeReplacer.TryUpgradeAsset(string.Empty, obj, out bool clearedMissing);
					bool cleaned = SRDuplicateCleaner.TryCleanupObject(obj, Settings.DuplicateMode);

					if ((modified || clearedMissing || cleaned) && !string.IsNullOrEmpty(scenePath))
					{
						var scene = SceneManager.GetSceneByPath(scenePath);
						if (scene.IsValid())
							EditorSceneManager.MarkSceneDirty(scene);
					}
				}

				if (reimportPaths.Count > 0)
				{
					foreach (var p in reimportPaths)
						InFlightImports.Add(p);

					int reimported = 0;
					foreach (var p in reimportPaths)
					{
						AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
						reimported++;
						if (reimported >= batchSize)
							break;
					}
				}

				if (dirtyAssets.Count > 0)
				{
					AssetDatabase.SaveAssets();
				}
				// If there is still work pending, schedule another batch
				if (PendingAssetPaths.Count > 0 || PendingSceneObjects.Count > 0)
				{
					ScheduleProcessing();
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"SRProcessingCoordinator error: {e}");
			}
			finally
			{
				_isProcessing = false;
			}
		}
	}
}
