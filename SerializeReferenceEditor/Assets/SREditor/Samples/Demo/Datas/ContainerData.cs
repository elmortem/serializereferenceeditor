using System;
using SerializeReferenceEditor;
using UnityEngine;

namespace Demo
{
	[Serializable]
	public class ContainerData
	{
		[SerializeReference, SR]
		public AbstractData Data;
	}
}