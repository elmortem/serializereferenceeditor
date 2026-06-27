using System;
using System.Collections.Generic;
using SerializeReferenceEditor.Editor.Settings;
using UnityEditor;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor
{
	public class AssetChangeDetector : AssetModificationProcessor
	{
		private static bool _onAssetSave;
		private static readonly HashSet<Object> _changedAssets = new();
		private static readonly List<SerializedObject> _selectionCache = new();
		private static bool _selectionDirty = true;
		private static double _lastPollTime;
		public static event Action<Object> ChangeEvent;

		public static void Initialize(bool onEditorUpdate, bool onUndoRedo, bool onAssetSave)
		{
			EditorApplication.update -= OnEditorUpdate;
			if (onEditorUpdate)
				EditorApplication.update += OnEditorUpdate;

			Undo.postprocessModifications -= OnUndoRedoPerformed;
			if (onUndoRedo)
				Undo.postprocessModifications += OnUndoRedoPerformed;

			Selection.selectionChanged -= OnSelectionChangedInternal;
			Selection.selectionChanged += OnSelectionChangedInternal;
			_selectionDirty = true;

			_onAssetSave = onAssetSave;
		}

		private static void OnSelectionChangedInternal()
		{
			_selectionDirty = true;
		}

		private static void OnEditorUpdate()
		{
			if (EditorApplication.isPlaying || EditorApplication.isPaused)
				return;

			int intervalMs = SREditorSettings.GetOrCreateSettings().ChangeDetectorPollIntervalMs;
			double now = EditorApplication.timeSinceStartup;
			if ((now - _lastPollTime) * 1000.0 < intervalMs)
				return;

			_lastPollTime = now;

			if (_selectionDirty)
			{
				RebuildSelectionCache();
				_selectionDirty = false;
			}

			for (int i = 0; i < _selectionCache.Count; i++)
			{
				var serializedObject = _selectionCache[i];
				if (serializedObject == null)
					continue;

				var target = serializedObject.targetObject;
				if (target == null)
					continue;

				if (serializedObject.UpdateIfRequiredOrScript())
				{
					if (_changedAssets.Add(target))
					{
						ChangeEvent?.Invoke(target);
						_changedAssets.Remove(target);
					}
				}
			}
		}

		private static void RebuildSelectionCache()
		{
			foreach (var serializedObject in _selectionCache)
			{
				serializedObject?.Dispose();
			}
			_selectionCache.Clear();

			foreach (var obj in Selection.objects)
			{
				if (obj == null)
					continue;

				_selectionCache.Add(new SerializedObject(obj));
			}
		}

		private static UndoPropertyModification[] OnUndoRedoPerformed(UndoPropertyModification[] modifications)
		{
			if (modifications == null || modifications.Length == 0)
				return modifications;

			foreach (var modification in modifications)
			{
				var target = modification.currentValue?.target;
				if (target != null && _changedAssets.Add(target))
				{
					ChangeEvent?.Invoke(target);
					_changedAssets.Remove(target);
				}
			}

			return modifications;
		}

		public static string[] OnWillSaveAssets(string[] paths)
		{
			if (!_onAssetSave)
				return paths;

			foreach (var path in paths)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
				if (asset != null && _changedAssets.Add(asset))
				{
					ChangeEvent?.Invoke(asset);
					_changedAssets.Remove(asset);
				}
			}

			return paths;
		}
	}
}
