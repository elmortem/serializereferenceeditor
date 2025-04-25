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
		
			int minLength = Math.Min(pathTypeA.Length, pathTypeB.Length);
			
			for (int i = 0; i < minLength; i++)
			{
				var compare = string.Compare(pathTypeA[i], pathTypeB[i], StringComparison.Ordinal);
				if (compare != 0)
				{
					return compare;
				}
			}
			
			return pathTypeA.Length.CompareTo(pathTypeB.Length);
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