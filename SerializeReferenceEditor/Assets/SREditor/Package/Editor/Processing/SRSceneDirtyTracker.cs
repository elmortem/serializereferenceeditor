using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.Processing
{
	public static class SRSceneDirtyTracker
	{
		private static readonly HashSet<string> ActiveScenes = new();
		private static readonly Dictionary<string, HashSet<int>> DirtyObjects = new();
		private static readonly HashSet<int> Empty = new();

		public static void MarkDirty(Object changedObject)
		{
			if (changedObject == null)
				return;

			var go = changedObject as GameObject;
			if (go == null && changedObject is Component component)
			{
				go = component.gameObject;
			}

			if (go == null)
				return;

			var scene = go.scene;
			if (!scene.IsValid())
				return;

			if (string.IsNullOrEmpty(scene.path))
				return;

			if (!DirtyObjects.TryGetValue(scene.path, out var set))
			{
				set = new HashSet<int>();
				DirtyObjects[scene.path] = set;
			}

			set.Add(changedObject.GetInstanceID());
		}

		public static bool ShouldProcessAll(string scenePath)
		{
			return !ActiveScenes.Contains(scenePath);
		}

		public static IReadOnlyCollection<int> GetDirtyObjectIds(string scenePath)
		{
			return DirtyObjects.TryGetValue(scenePath, out var set) ? set : Empty;
		}

		public static void OnProcessed(string scenePath)
		{
			if (string.IsNullOrEmpty(scenePath))
				return;

			ActiveScenes.Add(scenePath);
			if (DirtyObjects.TryGetValue(scenePath, out var set))
			{
				set.Clear();
			}
		}

		public static void Reset(string scenePath)
		{
			if (string.IsNullOrEmpty(scenePath))
				return;

			ActiveScenes.Remove(scenePath);
			DirtyObjects.Remove(scenePath);
		}
	}
}
