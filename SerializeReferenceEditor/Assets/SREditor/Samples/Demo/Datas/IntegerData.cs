using System;
using SerializeReferenceEditor;

namespace Demo
{
	[Serializable, SRName("Data/Simple types/Integer")]
	public class IntegerData : AbstractData
	{
		public int Int;
	}
}
