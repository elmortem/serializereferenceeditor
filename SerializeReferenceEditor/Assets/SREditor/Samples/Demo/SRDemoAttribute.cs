using System;
using SerializeReferenceEditor;

namespace Demo
{
	public class SRDemoAttribute : SRAttribute
	{
		public SRDemoAttribute() : base()
		{
		}

		public SRDemoAttribute(Type baseType) : base(baseType)
		{
		}

		public SRDemoAttribute(params Type[] types) : base(types)
		{
		}

		public override void OnCreate(object instance)
		{
			if(instance is AbstractNamedData namedData)
			{
				namedData.DataName = instance.GetType().Name;
			}
		}
	}
}
