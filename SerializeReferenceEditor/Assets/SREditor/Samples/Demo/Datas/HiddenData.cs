using System;
using SerializeReferenceEditor;

namespace Demo
{
	[Serializable, SRName("Data/Simple types/Hidden"), SRHidden]
	public class HiddenData : AbstractData
	{
		public int YouCantShowMe;
	}
}