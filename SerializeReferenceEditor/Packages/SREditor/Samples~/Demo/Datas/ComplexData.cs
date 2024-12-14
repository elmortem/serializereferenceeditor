using SerializeReferenceEditor;
using UnityEngine;

namespace Demo
{
	[SRName("Data/Custom types/Complex")]
	public class ComplexData : AbstractData
	{
		[SerializeReference, SR]
		public AbstractData Data;
		
		[SerializeReference, SRDemo]
		public AbstractData Demo;
	}
}