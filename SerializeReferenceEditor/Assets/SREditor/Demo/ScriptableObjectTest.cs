using System.Collections.Generic;
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
	}
}
