using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

namespace Demo
{
	[CreateAssetMenu(menuName = "Tools/SREditor/SRDemo/ScriptableObjectTest")]
	public class ScriptableObjectTest : ScriptableObject
	{
		public string Title;

		[SerializeReference]
		[SRDemo]
		public List<AbstractData> List;

		[SerializeReference, SR]
		public BaseTestData TestData;
		
		public CustomData CustomData;
	}
}
