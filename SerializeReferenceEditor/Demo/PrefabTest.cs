using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabTest : MonoBehaviour
{
	public string Title;

	public GameObject Child1;

	[SerializeReference]
	[SRDemo(typeof(Data1), typeof(Data2))]
	public List<AbstractData> DataOneTwoList;

	public GameObject Child2;

	public List<DataList> DataLists;

	public GameObject Child3;
}
