using System;
using SerializeReferenceEditor;

namespace Demo
{
	[Serializable, SRName("Data/Simple types/String")]
	public class StringData : AbstractData
	{
		public string Str;
	}
}
