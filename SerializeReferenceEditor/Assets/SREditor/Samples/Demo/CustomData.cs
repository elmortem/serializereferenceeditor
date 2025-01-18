using System;
using Demo;
using UnityEngine;

namespace Demo
{
	[Serializable]
	public class CustomData
	{
		[SerializeReference]
		public AbstractData Data;
	}
}