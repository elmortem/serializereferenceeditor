using System.Collections.Generic;

namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public readonly struct SRFileVerdict
	{
		public readonly bool HasManagedReferences;
		public readonly bool HasDuplicateRids;
		public readonly bool HasUnparsableTypes;
		public readonly List<SRTypeTriple> TypeTriples;

		public SRFileVerdict(bool hasManagedReferences, bool hasDuplicateRids, bool hasUnparsableTypes, List<SRTypeTriple> typeTriples)
		{
			HasManagedReferences = hasManagedReferences;
			HasDuplicateRids = hasDuplicateRids;
			HasUnparsableTypes = hasUnparsableTypes;
			TypeTriples = typeTriples;
		}
	}
}
