using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SerializeReferenceEditor
{
#if UNITY_EDITOR
	public static class SRTypeCache
	{
		private static readonly Dictionary<FormerlySerializedTypeAttribute, Type> _attributeTypes = new();

		static SRTypeCache()
		{
			CollectTypeReplacements();
		}

		private static void CollectTypeReplacements()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				CollectTypeReplacementsForAssembly(assembly.Location);
			}
		}

		public static void CollectTypeReplacementsForAssembly(string assemblyPath)
		{
			var assemblyName = System.IO.Path.GetFileNameWithoutExtension(assemblyPath);
			
			// Remove old attributes from this assembly
			var newAttributeTypes = _attributeTypes
				.Where(kvp => kvp.Value.Assembly.GetName().Name != assemblyName)
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			
			_attributeTypes.Clear();
			foreach (var kvp in newAttributeTypes)
			{
				_attributeTypes[kvp.Key] = kvp.Value;
			}

			try
			{
				var assembly = Assembly.LoadFrom(assemblyPath);
				var types = assembly.GetTypes();

				foreach (var type in types)
				{
					var attributes = type.GetCustomAttributes<FormerlySerializedTypeAttribute>();
					foreach (var attr in attributes)
					{
						_attributeTypes[attr] = type;
					}
				}
			}
			catch
			{
			}
		}

		public static Type GetTypeForAttribute(FormerlySerializedTypeAttribute attribute)
		{
			return _attributeTypes.GetValueOrDefault(attribute);
		}

		public static Type GetReplacementType(string assemblyName, string typeName)
		{
			if (string.IsNullOrEmpty(typeName))
				return null;

			return _attributeTypes
				.Where(kvp => 
					(string.IsNullOrEmpty(assemblyName) || kvp.Key.OldAssemblyName == assemblyName) &&
					$"{kvp.Key.OldNamespace}.{kvp.Key.OldTypeName}".TrimStart('.') == typeName)
				.Select(kvp => kvp.Value)
				.FirstOrDefault();
		}

		public static IEnumerable<(string oldAssembly, string oldType, Type newType)> GetAllReplacements()
		{
			return _attributeTypes.Select(kvp => 
				(kvp.Key.OldAssemblyName, 
				$"{kvp.Key.OldNamespace}.{kvp.Key.OldTypeName}".TrimStart('.'), 
				kvp.Value));
		}
	}
#endif
}