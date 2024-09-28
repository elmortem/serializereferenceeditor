using UnityEngine;

namespace Demo
{
	public class DataHolder : MonoBehaviour
	{
		[SerializeReference]
		[SRDemo(typeof(StringData), typeof(IntegerData))]
		public AbstractData[] DataOneTwoArray;

		[SerializeReference]
		[SRDemo(typeof(AbstractData))]
		public AbstractData[] DataAllArray;

		[SerializeReference]
		[SRDemo]
		public AbstractData DataSingle;
	}
}
