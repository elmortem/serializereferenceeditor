using System.Collections.Generic;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.Processing
{
	public static class SRSceneDirtyTracker
	{
		private static readonly HashSet<string> ActiveScenes = new();
		private static readonly Dictionary<string, HashSet<int>> DirtyRoots = new();

		public static void MarkDirty(GameObject root)
		{
			if (root == null)
				return;

			var scene = root.scene;
			if (!scene.IsValid())
				return;

			if (string.IsNullOrEmpty(scene.path))
				return;

			if (!DirtyRoots.TryGetValue(scene.path, out var set))
			{
				set = new HashSet<int>();
				DirtyRoots[scene.path] = set;
			}

			set.Add(root.GetInstanceID());
		}

		public static bool ShouldProcessAll(string scenePath)
		{
			return !ActiveScenes.Contains(scenePath);
		}

		public static bool IsRootDirty(string scenePath, int rootInstanceId)
		{
			return DirtyRoots.TryGetValue(scenePath, out var set) && set.Contains(rootInstanceId);
		}

		public static void OnProcessed(string scenePath)
		{
			if (string.IsNullOrEmpty(scenePath))
				return;

			ActiveScenes.Add(scenePath);
			if (DirtyRoots.TryGetValue(scenePath, out var set))
			{
				set.Clear();
			}
		}

		public static void Reset(string scenePath)
		{
			if (string.IsNullOrEmpty(scenePath))
				return;

			ActiveScenes.Remove(scenePath);
			DirtyRoots.Remove(scenePath);
		}
	}
}
