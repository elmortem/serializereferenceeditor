using System.Collections.Generic;
using SerializeReferenceEditor.Editor.Services;

namespace SerializeReferenceEditor.Editor.Comparers
{
    public class TypeInfoArrayComparer : IEqualityComparer<TypeInfo[]>
    {
        private static readonly TypeInfoComparer ElementComparer = new();

        public bool Equals(TypeInfo[] first, TypeInfo[] second)
        {
            if (first == second)
                return true;

            if (first == null || second == null)
                return false;

            if (first.Length != second.Length)
                return false;

            for (int i = 0; i < first.Length; i++)
                if (!ElementComparer.Equals(first[i], second[i]))
                    return false;

            return true;
        }

        public int GetHashCode(TypeInfo[] array)
        {
            var hash = 17;
            foreach (var element in array) 
                hash = hash * 31 + ElementComparer.GetHashCode(element);
            return hash;
        }
    }
}