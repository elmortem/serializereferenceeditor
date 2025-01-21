using System;
using SerializeReferenceEditor;

namespace Demo
{
	[Serializable, SRName("Data/Custom types/ScriptableObjectTest")]
	public class ScriptableObjectTestData : AbstractData
	{
		public ScriptableObjectTest Test;
	}
}
