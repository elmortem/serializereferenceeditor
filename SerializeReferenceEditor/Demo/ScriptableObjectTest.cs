using System.Collections.Generic;
using UnityEngine;

namespace SerializeReferenceEditor.Demo
{
	[CreateAssetMenu(menuName = "Tools/SREditor/SRDemo/ScriptableObjectTest")]
	public class ScriptableObjectTest : ScriptableObject
	{
		public string Title;

		[SerializeReference]
		[SRDemo(typeof(AbstractData))]
		public List<AbstractData> List;
	}
}
