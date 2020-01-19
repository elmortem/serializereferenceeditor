using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataHolder : MonoBehaviour
{
	[SerializeReference]
	[SRDemo(typeof(AbstractData))]
	public AbstractData[] DataArray;

	[SerializeReference]
	[SRDemo(typeof(AbstractData))]
	public AbstractData DataSingle;
}
