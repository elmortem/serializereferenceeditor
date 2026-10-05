using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.Processing
{
	public static class SRSceneDirtyTracker
	{
		private static readonly HashSet<string> ActiveScenes = new();
#if UNITY_6000_5_OR_NEWER
        private static readonly Dictionary<string, HashSet<EntityId>> DirtyObjects = new();
        private static readonly HashSet<EntityId> Empty = new();
#else
		private static readonly Dictionary<string, HashSet<int>> DirtyObjects = new();
		private static readonly HashSet<int> Empty = new();
#endif

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
				set = new();
				DirtyObjects[scene.path] = set;
			}

#if UNITY_6000_5_OR_NEWER
            set.Add(changedObject.GetEntityId());
#else
			set.Add(changedObject.GetInstanceID());
#endif
        }

        public static bool ShouldProcessAll(string scenePath)
		{
			return !ActiveScenes.Contains(scenePath);
		}

#if UNITY_6000_5_OR_NEWER
        public static IReadOnlyCollection<EntityId> GetDirtyObjectIds(string scenePath)
#else
		public static IReadOnlyCollection<int> GetDirtyObjectIds(string scenePath)
#endif
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
