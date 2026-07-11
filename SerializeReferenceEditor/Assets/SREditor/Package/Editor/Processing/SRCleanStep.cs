using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.Processing
{
	public class SRCleanStep
	{
		public readonly Object Target;
		public readonly SRCleanStepKind Kind;
		public readonly HashSet<object> SeenObjects;
		public readonly SRCleanGroup Group;

		public SRCleanStep(Object target, SRCleanStepKind kind, HashSet<object> seenObjects, SRCleanGroup group)
		{
			Target = target;
			Kind = kind;
			SeenObjects = seenObjects;
			Group = group;
		}
	}
}
