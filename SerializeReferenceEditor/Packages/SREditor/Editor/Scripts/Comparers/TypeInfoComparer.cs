using System;
using System.Collections.Generic;

namespace SerializeReferenceEditor.Editor.Comparers
{
    public class TypeInfoComparer : IEqualityComparer<TypeInfo>, IComparer<TypeInfo>
    {
        public int Compare(TypeInfo first, TypeInfo second)
        {
            if (ReferenceEquals(first, second))
            {
                return 0;
            }
            if (ReferenceEquals(null, second))
            {
                return 1;
            }
            if (ReferenceEquals(null, first))
            {
                return -1;
            }

            var pathTypeA = first.Path.Split('/');
            var pathTypeB = second.Path.Split('/');

            for (int i = 0; i < pathTypeA.Length; i++)
            {
                if (i > pathTypeB.Length)
                {
                    return 1;
                }

                var compare = string.Compare(pathTypeA[i], pathTypeB[i], StringComparison.Ordinal);
                if (compare == 0)
                {
                    continue;
                }

                if (pathTypeA.Length != pathTypeB.Length
                    && (i == pathTypeA.Length - 1
                        || i == pathTypeB.Length - 1))
                {
                    return pathTypeA.Length < pathTypeB.Length ? 1 : -1;
                }

                return compare;
            }

            return 0;
        }

        public bool Equals(TypeInfo first, TypeInfo second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }
            if (ReferenceEquals(first, null))
            {
                return false;
            }
            if (ReferenceEquals(second, null))
            {
                return false;
            }
            if (first.GetType() != second.GetType())
            {
                return false;
            }
            return first.Type == second.Type && first.Path == second.Path;
        }

        public int GetHashCode(TypeInfo obj)
        {
            if (obj == null)
            {
                return 0;
            }
            return HashCode.Combine(obj.Type, obj.Path);
        }
    }
}