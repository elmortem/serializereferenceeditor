using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.Processing
{
	public class SRCleanGroup
	{
		public readonly Object Root;
		public readonly string ScenePath;
		public int Remaining;
		public bool AnyChanged;

		public SRCleanGroup(Object root, string scenePath)
		{
			Root = root;
			ScenePath = scenePath;
		}
	}
}
