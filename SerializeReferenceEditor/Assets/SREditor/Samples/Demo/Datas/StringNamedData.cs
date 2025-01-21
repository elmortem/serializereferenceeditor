using System;
using SerializeReferenceEditor;

namespace Demo
{
	[Serializable, SRName("Data/Simple types/Named String")]
	public class StringNamedData : AbstractNamedData
	{
		public string Value;
	}
}