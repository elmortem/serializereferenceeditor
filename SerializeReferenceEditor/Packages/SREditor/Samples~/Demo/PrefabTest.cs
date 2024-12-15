using System;
using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

namespace Demo
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
		
		[SerializeReference]
		[SRDemo(typeof(IData))]
		public List<IData> IterfaceDataList;
		
		[SerializeReference, SR]
		public AbstractData ComplexData;

		[SerializeReference, SR]
		public BaseTestData TestData;
		
		public ContainerData[] Containers = Array.Empty<ContainerData>();
	}
}
