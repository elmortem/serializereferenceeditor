using System;
using System.Collections.Generic;
using UnityEngine;

namespace SerializeReferenceEditor.Demo
{
	[Serializable]
	public class DataList
	{
		public string Title;

		[SerializeReference]
		[SRDemo(typeof(AbstractData))]
		public List<AbstractData> List;
	}
}
