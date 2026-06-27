using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SerializeReferenceEditor.Editor.Processing.DoubleClean;
using SerializeReferenceEditor.Editor.Processing.TypeReplace;
using SerializeReferenceEditor.Editor.Settings;
using SerializeReferenceEditor.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.Processing
{
	public class ProcessingCoordinator : AssetPostprocessor
	{
		private static readonly HashSet<string> PendingAssetPaths = new();
		private static readonly HashSet<Object> PendingSceneObjects = new();
		private static readonly HashSet<string> InFlightImports = new();

		private static readonly List<SRFileScanResult> PendingWrites = new();
		private static readonly List<string> PendingObjBranch = new();
		private static readonly List<string> ReimportQueue = new();
		private static readonly List<Object> SceneApplyQueue = new();
		private static readonly List<Object> DirtyAssets = new();

		private static SRProcessingState _state = SRProcessingState.Idle;
		private static bool _pumpSubscribed;

		private static CancellationTokenSource _scanCts;
		private static Task _scanTask;
		private static ConcurrentQueue<SRFileScanResult> _scanResults;

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

			AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static SREditorSettings Settings => SREditorSettings.GetOrCreateSettings();

		private static bool IsProcessableAssetPath(string path)
		{
			if (string.IsNullOrEmpty(path))
				return false;

			return path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
				   || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
				   || path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase);
		}

		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
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
				if (!IsProcessableAssetPath(path))
					continue;

				if (InFlightImports.Remove(path))
					continue;

				EnqueueAssetPath(path);
			}

			foreach (var path in movedAssets)
			{
				if (!IsProcessableAssetPath(path))
					continue;

				if (InFlightImports.Remove(path))
					continue;

				EnqueueAssetPath(path);
			}
		}

		private static void OnAssetChanged(Object changedObject)
		{
			if (_state == SRProcessingState.Applying)
				return;

			if (changedObject == null)
				return;

			var assetPath = AssetDatabase.GetAssetPath(changedObject);
			if (IsProcessableAssetPath(assetPath))
			{
				EnqueueAssetPath(assetPath);
				return;
			}

			var root = ResolveSceneRoot(changedObject);
			if (root != null)
			{
				SRSceneDirtyTracker.MarkDirty(root);
			}
		}

		private static GameObject ResolveSceneRoot(Object obj)
		{
			var go = obj as GameObject;
			if (go == null && obj is Component component)
			{
				go = component.gameObject;
			}

			if (go == null)
				return null;

			if (!go.scene.IsValid())
				return null;

			return go.transform.root.gameObject;
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
					continue;

				if (!AssetDatabase.IsMainAsset(obj))
					continue;

				EnqueueAssetPath(path);
			}
		}

		private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
		{
			SRSceneDirtyTracker.Reset(scene.path);

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
			{
				EnsurePump();
			}
		}

		private static void EnqueueSceneObject(Object obj)
		{
			if (obj == null)
				return;

			if (PendingSceneObjects.Add(obj))
			{
				EnsurePump();
			}
		}

		private static void OnSceneSaving(Scene scene, string path)
		{
			if (!Settings.FormerlySerializedTypeOnSceneSave)
				return;

			bool processAll = SRSceneDirtyTracker.ShouldProcessAll(path);
			var roots = scene.GetRootGameObjects();
			bool anyChanged = false;

			foreach (var go in roots)
			{
				if (go == null)
					continue;

				if (!processAll && !SRSceneDirtyTracker.IsRootDirty(path, go.GetInstanceID()))
					continue;

				TypeReplacer.TryClearMissingReferences(go, out bool clearedMissing);
				bool cleaned = SRDuplicateCleaner.TryCleanupObject(go, Settings.DuplicateMode);
				anyChanged |= clearedMissing | cleaned;
			}

			if (anyChanged)
			{
				EditorSceneManager.MarkSceneDirty(scene);
			}

			SRSceneDirtyTracker.OnProcessed(path);
		}

		private static void EnsurePump()
		{
			if (_pumpSubscribed)
				return;

			_pumpSubscribed = true;
			EditorApplication.update += Pump;
		}

		private static void UnsubscribePump()
		{
			if (!_pumpSubscribed)
				return;

			_pumpSubscribed = false;
			EditorApplication.update -= Pump;
		}

		private static void Pump()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				return;

			switch (_state)
			{
				case SRProcessingState.Idle:
					PumpIdle();
					break;

				case SRProcessingState.Scanning:
					if (_scanTask != null && _scanTask.IsCompleted)
					{
						BeginApply();
					}
					break;

				case SRProcessingState.Applying:
					StepApply();
					break;
			}
		}

		private static void PumpIdle()
		{
			if (PendingAssetPaths.Count > 0)
			{
				StartScan();
				return;
			}

			if (PendingSceneObjects.Count > 0)
			{
				BeginApply();
				return;
			}

			UnsubscribePump();
		}

		private static List<SRReplacementPattern> SnapshotPatterns()
		{
			var list = new List<SRReplacementPattern>();
			foreach (var (oldAssembly, oldType, newType) in SRFormerlyTypeCache.GetAllReplacements())
			{
				var oldTypePattern = string.IsNullOrEmpty(oldAssembly) ? oldType : $"{oldAssembly}, {oldType}";
				var newAssembly = newType.Assembly.GetName().Name;
				var newTypePattern = string.IsNullOrEmpty(newAssembly) ? newType.FullName : $"{newAssembly}, {newType.FullName}";
				list.Add(SRReplacementPattern.Parse(oldTypePattern, newTypePattern));
			}

			return list;
		}

		private static void StartScan()
		{
			var patterns = SnapshotPatterns();
			var paths = new List<string>(PendingAssetPaths);
			PendingAssetPaths.Clear();

			if (patterns.Count == 0)
			{
				foreach (var p in paths)
				{
					PendingObjBranch.Add(p);
				}

				_scanResults = null;
				BeginApply();
				return;
			}

			_scanResults = new ConcurrentQueue<SRFileScanResult>();
			_scanCts = new CancellationTokenSource();
			var token = _scanCts.Token;
			var results = _scanResults;
			int maxThreads = Settings.ProcessingMaxThreads;

			_scanTask = Task.Run(() =>
			{
				var options = new ParallelOptions { MaxDegreeOfParallelism = maxThreads, CancellationToken = token };
				try
				{
					Parallel.ForEach(paths, options, p =>
					{
						token.ThrowIfCancellationRequested();
						results.Enqueue(SRFileScanner.Scan(p, patterns));
					});
				}
				catch (OperationCanceledException)
				{
				}
				catch (Exception e)
				{
					Debug.LogError($"SRProcessingCoordinator scan error: {e}");
				}
			}, token);

			_state = SRProcessingState.Scanning;
		}

		private static void BeginApply()
		{
			if (_scanResults != null)
			{
				while (_scanResults.TryDequeue(out var result))
				{
					if (result.Modified)
					{
						PendingWrites.Add(result);
					}
					else
					{
						PendingObjBranch.Add(result.Path);
					}
				}
			}

			foreach (var obj in PendingSceneObjects)
			{
				if (obj != null)
				{
					SceneApplyQueue.Add(obj);
				}
			}
			PendingSceneObjects.Clear();

			_scanResults = null;
			_scanTask = null;
			DisposeCts();
			_state = SRProcessingState.Applying;
		}

		private static void StepApply()
		{
			var settings = Settings;
			long budgetMs = settings.ProcessingFrameBudgetMs;
			var sw = Stopwatch.StartNew();

			while (PendingWrites.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
			{
				var result = PendingWrites[^1];
				PendingWrites.RemoveAt(PendingWrites.Count - 1);
				try
				{
					File.WriteAllText(result.Path, result.NewContent);
					ReimportQueue.Add(result.Path);
				}
				catch (Exception e)
				{
					Debug.LogError($"SRProcessingCoordinator write error: {e}");
				}
			}

			bool wantClearMissing = settings.ClearMissingReferencesIfNoReplacement;
			bool wantDuplicateClean = settings.DuplicateMode != SRDuplicateMode.Default;

			while (PendingObjBranch.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
			{
				var path = PendingObjBranch[^1];
				PendingObjBranch.RemoveAt(PendingObjBranch.Count - 1);

				if (!wantClearMissing && !wantDuplicateClean)
					continue;

				var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
				if (asset == null)
					continue;

				bool cleaned = false;
				bool cleared = false;
				if (wantDuplicateClean)
				{
					cleaned = SRDuplicateCleaner.TryCleanupObject(asset, settings.DuplicateMode);
				}

				if (wantClearMissing)
				{
					TypeReplacer.TryClearMissingReferences(asset, out cleared);
				}

				if (cleaned || cleared)
				{
					EditorUtility.SetDirty(asset);
					DirtyAssets.Add(asset);
				}
			}

			while (SceneApplyQueue.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
			{
				var obj = SceneApplyQueue[^1];
				SceneApplyQueue.RemoveAt(SceneApplyQueue.Count - 1);
				if (obj == null)
					continue;

				ProcessSceneObject(obj, settings);
			}

			if (ReimportQueue.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
			{
				int chunk = settings.ProcessingBatchSize;
				int n = 0;
				AssetDatabase.StartAssetEditing();
				try
				{
					while (ReimportQueue.Count > 0 && n < chunk && sw.ElapsedMilliseconds < budgetMs)
					{
						var p = ReimportQueue[^1];
						ReimportQueue.RemoveAt(ReimportQueue.Count - 1);
						InFlightImports.Add(p);
						AssetDatabase.ImportAsset(p);
						n++;
					}
				}
				finally
				{
					AssetDatabase.StopAssetEditing();
				}
			}

			bool applyDone = PendingWrites.Count == 0
				&& PendingObjBranch.Count == 0
				&& SceneApplyQueue.Count == 0
				&& ReimportQueue.Count == 0;

			if (!applyDone)
				return;

			if (DirtyAssets.Count > 0)
			{
				AssetDatabase.SaveAssets();
				DirtyAssets.Clear();
			}

			_state = SRProcessingState.Idle;
		}

		private static void ProcessSceneObject(Object obj, SREditorSettings settings)
		{
			string scenePath = string.Empty;
			if (obj is Component c && c.gameObject.scene.IsValid())
			{
				scenePath = c.gameObject.scene.path;
			}
			else if (obj is GameObject go && go.scene.IsValid())
			{
				scenePath = go.scene.path;
			}

			TypeReplacer.TryClearMissingReferences(obj, out bool cleared);
			bool cleaned = SRDuplicateCleaner.TryCleanupObject(obj, settings.DuplicateMode);

			if ((cleared || cleaned) && !string.IsNullOrEmpty(scenePath))
			{
				var scene = SceneManager.GetSceneByPath(scenePath);
				if (scene.IsValid())
				{
					EditorSceneManager.MarkSceneDirty(scene);
				}
			}
		}

		private static void OnBeforeAssemblyReload()
		{
			CancelScan();
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange change)
		{
			if (change == PlayModeStateChange.ExitingEditMode)
			{
				CancelScan();
			}
		}

		private static void CancelScan()
		{
			if (_scanCts != null)
			{
				_scanCts.Cancel();
			}
		}

		private static void DisposeCts()
		{
			if (_scanCts != null)
			{
				_scanCts.Dispose();
				_scanCts = null;
			}
		}
	}
}
