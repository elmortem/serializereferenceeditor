using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class SRAttribute : PropertyAttribute
{
	private static Dictionary<Type, Type[]> _typeCache = new Dictionary<Type, Type[]>();

	public class TypeInfo
	{
		public Type Type;
		public string Path;
	}

	public TypeInfo[] Types { get; private set; }

	public SRAttribute(Type type)
	{
		if(type == null)
		{
			Debug.LogError("[SRAttribute] Incorrect type.");
		}

		Types = GetTypeInfos(GetChildTypes(type));
	}

	public SRAttribute(Type[] types)
	{
		if(types == null || types.Length <= 0)
		{
			Debug.LogError("[SRAttribute] Incorrect types.");
		}

		Types = GetTypeInfos(types);
	}

	public TypeInfo TypeInfoByPath(string path)
	{
		return Types != null ? Array.Find(Types, p => p.Path == path) : null;
	}

	public static TypeInfo[] GetTypeInfos(Type[] types)
	{
		if(types == null)
			return null;

		TypeInfo[] result = new TypeInfo[types.Length];

		for(int i = 0; i < types.Length; ++i)
		{
			result[i] = new TypeInfo { Type = types[i], Path = types[i].FullName };
		}

		return result;
	}

	public Type[] GetChildTypes(Type type)
	{
		Type[] result;
		//if(_typeCache.TryGetValue(type, out result))
		//	return result;

		if(type.IsInterface)
		{
			result = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => p != type && type.IsAssignableFrom(p)).ToArray();
		}
		else
		{
			result = Assembly.GetAssembly(type).GetTypes().Where(t => t.IsSubclassOf(type)).ToArray();

		}

		if(result != null)
			_typeCache[type] = result;

		return result;
	}

	public virtual void OnCreate(object instance)
	{

	}
}
