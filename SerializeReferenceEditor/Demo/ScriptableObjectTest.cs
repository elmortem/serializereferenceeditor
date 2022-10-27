using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SRDemo/ScriptableObjectTest")]
public class ScriptableObjectTest : ScriptableObject
{
	public string Title;

	[SerializeReference]
	[SRDemo(typeof(AbstractData))]
	public List<AbstractData> List;
}
