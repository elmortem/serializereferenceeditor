using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DataList
{
	public string Title;

	[SerializeReference]
	[SRDemo(typeof(AbstractData))]
	public List<AbstractData> List;
}
