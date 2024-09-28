using System;

namespace SerializeReferenceEditor
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SRNameAttribute : Attribute
    {
        public readonly string FullName;
        public readonly string Name;

        public SRNameAttribute(string fullName)
        {
            FullName = fullName;
            if (!fullName.Contains("/"))
            {
                Name = fullName;
                return;
            }

            var separateName = fullName.Split('/');
            Name = separateName[^1];
        }
    }
}