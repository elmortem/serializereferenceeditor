using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataHolder : MonoBehaviour
{
	[SerializeReference]
	[SRDemo(typeof(Data1), typeof(Data2))]
	public AbstractData[] DataOneTwoArray;

	[SerializeReference]
	[SRDemo(typeof(AbstractData))]
	public AbstractData[] DataAllArray;

	[SerializeReference]
	[SRDemo]
	public AbstractData DataSingle;
}
