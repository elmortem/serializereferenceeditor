using System;
using UnityEngine;

namespace SerializeReferenceEditor
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SRAttribute : PropertyAttribute
    {
        public Type[] Types { get; private set; }

        public SRAttribute()
        {
            Types = null;
        }

        public SRAttribute(Type baseType)
        {
            Types = new []{ baseType };
        }

        public SRAttribute(params Type[] types)
        {
            Types = types;
        }

        public virtual void OnCreate(object instance)
        {
            // Override this method to customize instance creation
        }
    }
}
