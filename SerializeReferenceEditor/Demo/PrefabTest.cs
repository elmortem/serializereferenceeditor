﻿using System.Collections.Generic;
using UnityEngine;

namespace SerializeReferenceEditor.Demo
{
	public class PrefabTest : MonoBehaviour
	{
		public string Title;

		public GameObject Child1;

		[SerializeReference]
		[SRDemo(typeof(AbstractData))]
		public AbstractData SingleElement;

		[SerializeReference]
		[SRDemo(typeof(StringData), typeof(IntegerData))]
		public List<AbstractData> StringOrIntegerTypesDataList;

		public GameObject Child2;

		public List<DataList> DataLists;

		public GameObject Child3;
	}
}
