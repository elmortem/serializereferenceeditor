using System;
using SerializeReferenceEditor;
using UnityEngine;

namespace Demo
{
	[Serializable, SRName("Data/Simple types/Named Float")]
	public class FloatNamedData : AbstractNamedData
	{
		[Range(0f, 1f)]
		public float Float;
	}
}