using System;
using System.Collections.Generic;
using System.Reflection;

namespace SerializeReferenceEditor.Editor.Processing.TypeReplace
{
	public static class SRTypeResolveCache
	{
		private static readonly Dictionary<SRTypeTriple, bool> Missing = new();
		private static Dictionary<string, Assembly> _assembliesByName;

		public static bool IsMissing(SRTypeTriple triple)
		{
			if (string.IsNullOrEmpty(triple.ClassName))
			{
				return true;
			}

			if (Missing.TryGetValue(triple, out bool missing))
			{
				return missing;
			}

			missing = ResolveType(triple) == null;
			Missing[triple] = missing;
			return missing;
		}

		private static Type ResolveType(SRTypeTriple triple)
		{
			if (_assembliesByName == null)
			{
				_assembliesByName = new Dictionary<string, Assembly>();
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					_assembliesByName[assembly.GetName().Name] = assembly;
				}
			}

			var typeName = string.IsNullOrEmpty(triple.Namespace)
				? triple.ClassName
				: triple.Namespace + "." + triple.ClassName;

			if (!string.IsNullOrEmpty(triple.Assembly) && _assembliesByName.TryGetValue(triple.Assembly, out var known))
			{
				return known.GetType(typeName);
			}

			foreach (var assembly in _assembliesByName.Values)
			{
				var type = assembly.GetType(typeName);
				if (type != null)
				{
					return type;
				}
			}

			return null;
		}
	}
}
