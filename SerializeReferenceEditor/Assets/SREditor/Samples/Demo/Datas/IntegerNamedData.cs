using System;
using SerializeReferenceEditor;

namespace Demo
{
	[Serializable, SRName("Data/Simple types/Named Integer")]
	public class IntegerNamedData : AbstractNamedData
	{
		public int Int;
	}
}