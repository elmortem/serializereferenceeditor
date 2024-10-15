using System;
using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

namespace Demo
{
	[Serializable]
	public class DataList
	{
		public string Title;

		[SerializeReference]
		[SR(typeof(AbstractData))]
		public List<AbstractData> List;
	}
}
