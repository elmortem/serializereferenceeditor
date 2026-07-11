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
		private static readonly HashSet<string> NeedsCleanAfterImport = new();

		private static readonly List<SRFileScanResult> PendingVerdicts = new();
		private static readonly List<SRFileScanResult> PendingWrites = new();
		private static readonly List<string> PendingObjBranch = new();
		private static readonly List<SRImportItem> ReimportQueue = new();
		private static readonly List<Object> SceneApplyQueue = new();
		private static readonly List<SRCleanStep> CleanSteps = new();
		private static readonly List<Object> DirtyAssets = new();

		private static SRProcessingState _state = SRProcessingState.Idle;
		private static bool _pumpSubscribed;

		private static CancellationTokenSource _cts;
		private static Task _scanTask;
		private static Task _writeTask;
		private static ConcurrentQueue<SRFileScanResult> _scanResults;
		private static ConcurrentQueue<SRImportItem> _writtenFiles;

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

			SRSceneDirtyTracker.MarkDirty(changedObject);
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

			var settings = Settings;
			bool wantMissing = settings.ClearMissingReferencesIfNoReplacement;
			bool wantDup = settings.DuplicateMode != SRDuplicateMode.None;
			bool anyChanged = false;

			if (SRSceneDirtyTracker.ShouldProcessAll(path))
			{
				var roots = scene.GetRootGameObjects();
				foreach (var go in roots)
				{
					if (go == null)
						continue;

					TypeReplacer.TryClearMissingReferences(go, out bool clearedMissing);
					bool cleaned = SRDuplicateCleaner.TryCleanupObject(go, settings.DuplicateMode);
					anyChanged |= clearedMissing | cleaned;
				}
			}
			else
			{
				anyChanged = ProcessDirtyObjects(path, wantMissing, wantDup, settings.DuplicateMode);
			}

			if (anyChanged)
			{
				EditorSceneManager.MarkSceneDirty(scene);
			}

			SRSceneDirtyTracker.OnProcessed(path);
		}

		private static bool ProcessDirtyObjects(string path, bool wantMissing, bool wantDup, SRDuplicateMode mode)
		{
			var ids = SRSceneDirtyTracker.GetDirtyObjectIds(path);
			if (ids.Count == 0)
				return false;

			bool anyChanged = false;
			var groups = new Dictionary<GameObject, List<Object>>();

			foreach (var id in ids)
			{
				var obj = EditorUtility.InstanceIDToObject(id);
				if (obj == null)
					continue;

				var go = obj as GameObject;
				if (go == null && obj is Component component)
				{
					go = component.gameObject;
				}

				if (go == null || !go.scene.IsValid() || go.scene.path != path)
					continue;

				if (!groups.TryGetValue(go, out var list))
				{
					list = new List<Object>();
					groups[go] = list;
				}

				list.Add(obj);
			}

			foreach (var pair in groups)
			{
				var go = pair.Key;
				var seen = new HashSet<object>();

				foreach (var obj in pair.Value)
				{
					if (obj is GameObject)
					{
						if (wantMissing)
						{
							TypeReplacer.TryClearMissingReferences(go, out bool cleared);
							anyChanged |= cleared;
						}

						if (wantDup)
						{
							foreach (var component in go.GetComponents<Component>())
							{
								if (component == null)
									continue;

								anyChanged |= SRDuplicateCleaner.TryCleanupTarget(component, mode, seen);
							}
						}
					}
					else
					{
						if (wantMissing)
						{
							anyChanged |= TypeReplacer.ClearMissingOn(obj);
						}

						if (wantDup)
						{
							anyChanged |= SRDuplicateCleaner.TryCleanupTarget(obj, mode, seen);
						}
					}
				}
			}

			return anyChanged;
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
						BeginClassify();
					}
					break;

				case SRProcessingState.Classifying:
					StepClassify();
					break;

				case SRProcessingState.Writing:
					DrainWrittenFiles();
					if (_writeTask != null && _writeTask.IsCompleted)
					{
						DrainWrittenFiles();
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

			_scanResults = new ConcurrentQueue<SRFileScanResult>();
			_cts = new CancellationTokenSource();
			var token = _cts.Token;
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

		private static void BeginClassify()
		{
			while (_scanResults.TryDequeue(out var result))
			{
				PendingVerdicts.Add(result);
			}

			_scanResults = null;
			_scanTask = null;
			_state = SRProcessingState.Classifying;
		}

		private static void StepClassify()
		{
			var settings = Settings;
			long budgetMs = settings.ProcessingFrameBudgetMs;
			var sw = Stopwatch.StartNew();

			bool wantMissing = settings.ClearMissingReferencesIfNoReplacement;
			bool wantDup = settings.DuplicateMode != SRDuplicateMode.None;

			while (PendingVerdicts.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
			{
				var result = PendingVerdicts[^1];
				PendingVerdicts.RemoveAt(PendingVerdicts.Count - 1);

				bool needsClean = NeedsObjectClean(result, wantMissing, wantDup);

				if (result.Modified)
				{
					PendingWrites.Add(result);
					if (needsClean)
					{
						NeedsCleanAfterImport.Add(result.Path);
					}

					continue;
				}

				if (needsClean)
				{
					PendingObjBranch.Add(result.Path);
				}
			}

			if (PendingVerdicts.Count > 0)
				return;

			if (PendingWrites.Count > 0)
			{
				StartWrites();
				return;
			}

			DisposeCts();
			BeginApply();
		}

		private static bool NeedsObjectClean(in SRFileScanResult result, bool wantMissing, bool wantDup)
		{
			if (!result.Verdict.HasManagedReferences)
				return false;

			if (wantDup && result.Verdict.HasDuplicateRids)
				return true;

			if (!wantMissing)
				return false;

			if (result.Verdict.HasUnparsableTypes)
				return true;

			if (result.Verdict.TypeTriples != null)
			{
				foreach (var triple in result.Verdict.TypeTriples)
				{
					if (SRTypeResolveCache.IsMissing(triple))
						return true;
				}
			}

			return false;
		}

		private static void StartWrites()
		{
			_writtenFiles = new ConcurrentQueue<SRImportItem>();
			var writes = new List<SRFileScanResult>(PendingWrites);
			PendingWrites.Clear();
			var written = _writtenFiles;
			var token = _cts.Token;

			_writeTask = Task.Run(() =>
			{
				foreach (var result in writes)
				{
					if (token.IsCancellationRequested)
						return;

					try
					{
						var tempPath = result.Path + ".srtmp";
						File.WriteAllText(tempPath, result.NewContent);

						if (File.Exists(result.Path))
						{
							try
							{
								File.Replace(tempPath, result.Path, null);
							}
							catch (IOException)
							{
								File.Delete(result.Path);
								File.Move(tempPath, result.Path);
							}
						}
						else
						{
							File.Move(tempPath, result.Path);
						}

						long size = new FileInfo(result.Path).Length;
						written.Enqueue(new SRImportItem(result.Path, size));
					}
					catch (Exception e)
					{
						Debug.LogError($"SRProcessingCoordinator write error ({result.Path}): {e}");
					}
				}
			}, token);

			_state = SRProcessingState.Writing;
		}

		private static void DrainWrittenFiles()
		{
			if (_writtenFiles == null)
				return;

			while (_writtenFiles.TryDequeue(out var item))
			{
				ReimportQueue.Add(item);
			}
		}

		private static void BeginApply()
		{
			_writtenFiles = null;
			_writeTask = null;
			DisposeCts();

			foreach (var obj in PendingSceneObjects)
			{
				if (obj != null)
				{
					SceneApplyQueue.Add(obj);
				}
			}
			PendingSceneObjects.Clear();

			_state = SRProcessingState.Applying;
		}

		private static void StepApply()
		{
			var settings = Settings;
			long budgetMs = settings.ProcessingFrameBudgetMs;
			var sw = Stopwatch.StartNew();

			bool wantMissing = settings.ClearMissingReferencesIfNoReplacement;
			bool wantDup = settings.DuplicateMode != SRDuplicateMode.None;

			while (PendingObjBranch.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
			{
				var path = PendingObjBranch[^1];
				PendingObjBranch.RemoveAt(PendingObjBranch.Count - 1);

				var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
				if (asset == null)
					continue;

				if (asset is SceneAsset)
					continue;

				ExpandCleanSteps(asset, string.Empty, wantMissing, wantDup);
			}

			while (SceneApplyQueue.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
			{
				var obj = SceneApplyQueue[^1];
				SceneApplyQueue.RemoveAt(SceneApplyQueue.Count - 1);
				if (obj == null)
					continue;

				ExpandCleanSteps(obj, ResolveScenePath(obj), wantMissing, wantDup);
			}

			while (CleanSteps.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
			{
				var step = CleanSteps[^1];
				CleanSteps.RemoveAt(CleanSteps.Count - 1);
				ProcessCleanStep(step, settings);
			}

			if (ReimportQueue.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
			{
				long chunkBytes = settings.ProcessingImportChunkKb * 1024L;
				int chunkCount = settings.ProcessingBatchSize;
				long accBytes = 0;
				int n = 0;
				AssetDatabase.StartAssetEditing();
				try
				{
					while (ReimportQueue.Count > 0 && n < chunkCount && sw.ElapsedMilliseconds < budgetMs)
					{
						var item = ReimportQueue[^1];
						if (n > 0 && accBytes + item.SizeBytes > chunkBytes)
							break;

						ReimportQueue.RemoveAt(ReimportQueue.Count - 1);
						InFlightImports.Add(item.Path);
						AssetDatabase.ImportAsset(item.Path);
						if (NeedsCleanAfterImport.Remove(item.Path))
						{
							PendingObjBranch.Add(item.Path);
						}

						accBytes += item.SizeBytes;
						n++;
					}
				}
				finally
				{
					AssetDatabase.StopAssetEditing();
				}
			}

			bool workLeft = PendingObjBranch.Count > 0
				|| SceneApplyQueue.Count > 0
				|| CleanSteps.Count > 0
				|| ReimportQueue.Count > 0;

			if (workLeft)
				return;

			while (DirtyAssets.Count > 0 && sw.ElapsedMilliseconds < budgetMs)
			{
				var asset = DirtyAssets[^1];
				DirtyAssets.RemoveAt(DirtyAssets.Count - 1);
				if (asset == null)
					continue;

				var path = AssetDatabase.GetAssetPath(asset);
				if (!string.IsNullOrEmpty(path))
				{
					InFlightImports.Add(path);
				}

				AssetDatabase.SaveAssetIfDirty(asset);
			}

			if (DirtyAssets.Count > 0)
				return;

			_state = SRProcessingState.Idle;
		}

		private static void ExpandCleanSteps(Object target, string scenePath, bool wantMissing, bool wantDup)
		{
			var group = new SRCleanGroup(target, scenePath);
			int before = CleanSteps.Count;

			if (target is GameObject go)
			{
				if (wantDup)
				{
					var seen = new HashSet<object>();
					foreach (var component in go.GetComponents<Component>())
					{
						if (component == null)
							continue;

						CleanSteps.Add(new SRCleanStep(component, SRCleanStepKind.Duplicate, seen, group));
					}
				}

				if (wantMissing)
				{
					foreach (var component in go.GetComponentsInChildren<MonoBehaviour>(true))
					{
						if (component == null)
							continue;

						CleanSteps.Add(new SRCleanStep(component, SRCleanStepKind.Missing, null, group));
					}
				}
			}
			else
			{
				if (wantDup)
				{
					CleanSteps.Add(new SRCleanStep(target, SRCleanStepKind.Duplicate, new HashSet<object>(), group));
				}

				if (wantMissing)
				{
					CleanSteps.Add(new SRCleanStep(target, SRCleanStepKind.Missing, null, group));
				}
			}

			group.Remaining = CleanSteps.Count - before;
		}

		private static void ProcessCleanStep(SRCleanStep step, SREditorSettings settings)
		{
			var target = step.Target;
			if (target == null)
			{
				FinishStep(step.Group, false);
				return;
			}

			bool changed = false;
			switch (step.Kind)
			{
				case SRCleanStepKind.Duplicate:
					changed = SRDuplicateCleaner.TryCleanupTarget(target, settings.DuplicateMode, step.SeenObjects);
					break;

				case SRCleanStepKind.Missing:
					changed = TypeReplacer.ClearMissingOn(target);
					break;
			}

			FinishStep(step.Group, changed);
		}

		private static void FinishStep(SRCleanGroup group, bool changed)
		{
			group.AnyChanged |= changed;
			group.Remaining--;
			if (group.Remaining > 0)
				return;

			if (!group.AnyChanged)
				return;

			if (string.IsNullOrEmpty(group.ScenePath))
			{
				if (group.Root != null)
				{
					EditorUtility.SetDirty(group.Root);
					if (!DirtyAssets.Contains(group.Root))
					{
						DirtyAssets.Add(group.Root);
					}
				}
			}
			else
			{
				var scene = SceneManager.GetSceneByPath(group.ScenePath);
				if (scene.IsValid())
				{
					EditorSceneManager.MarkSceneDirty(scene);
				}
			}
		}

		private static string ResolveScenePath(Object obj)
		{
			if (obj is Component component && component.gameObject.scene.IsValid())
				return component.gameObject.scene.path;

			if (obj is GameObject go && go.scene.IsValid())
				return go.scene.path;

			return string.Empty;
		}

		private static void OnBeforeAssemblyReload()
		{
			CancelProcessing();
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange change)
		{
			if (change == PlayModeStateChange.ExitingEditMode)
			{
				CancelProcessing();
			}
		}

		private static void CancelProcessing()
		{
			if (_cts != null)
			{
				_cts.Cancel();
			}
		}

		private static void DisposeCts()
		{
			if (_cts != null)
			{
				_cts.Dispose();
				_cts = null;
			}
		}
	}
}
