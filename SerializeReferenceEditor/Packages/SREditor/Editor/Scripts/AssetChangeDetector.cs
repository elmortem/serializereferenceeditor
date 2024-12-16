using System;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor
{
	public class AssetChangeDetector : AssetModificationProcessor
	{
		private static bool _onAssetSave;
		private static readonly HashSet<Object> _changedAssets = new();
		public static event Action<Object> ChangeEvent;
		
		public static void Initialize(bool onEditorUpdate, bool onUndoRedo, bool onAssetSave)
		{
			EditorApplication.update -= OnEditorUpdate;
			if (onEditorUpdate)
				EditorApplication.update += OnEditorUpdate;
			
			Undo.postprocessModifications -= OnUndoRedoPerformed;
			if (onUndoRedo)
				Undo.postprocessModifications += OnUndoRedoPerformed;
			
			_onAssetSave = onAssetSave;
		}

		private static void OnEditorUpdate()
		{
			if (EditorApplication.isPlaying || EditorApplication.isPaused)
				return;

			foreach (var obj in Selection.objects)
			{
				if (obj == null)
					continue;

				var serializedObject = new SerializedObject(obj);
				if (serializedObject.UpdateIfRequiredOrScript())
				{
					if (_changedAssets.Add(obj))
					{
						ChangeEvent?.Invoke(obj);
						_changedAssets.Remove(obj);
					}
				}
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