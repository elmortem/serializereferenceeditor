using UnityEngine;

namespace SerializeReferenceEditor.Demo
{
	[SRName("Data/Simple types/Float")]
	public class FloatData : AbstractData
	{
		[Range(0f, 1f)]
		public float Float;
	}
}
