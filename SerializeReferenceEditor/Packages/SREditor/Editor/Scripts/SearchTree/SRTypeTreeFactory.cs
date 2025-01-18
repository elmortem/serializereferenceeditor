using System;
using System.Collections.Generic;
using System.Linq;
using SerializeReferenceEditor.Editor.Comparers;
using SerializeReferenceEditor.Editor.SRActions;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.Drawers
{
	public class SRTypeTreeFactory
	{
		private readonly string[] _sortedTypePath;
		private readonly NameService _nameService = new();

		public SRTypeTreeFactory(TypeInfo[] types)
		{
			Array.Sort(types, new TypeInfoComparer());
			_sortedTypePath = types.Select(info => info.Path).ToArray();
		}

		public List<SearchTreeEntry> MakeTypesTree(SRActionFactory srActionFactory)
		{
			List<SearchTreeEntry> list = new();
			var groups = new List<string>();
			foreach (var pathType in _sortedTypePath)
			{
				var splitPathType = _nameService.GetSplitPathType(pathType);
				var groupName = "";
				for (int i = 0; i < splitPathType.Length - 1; i++)
				{
					groupName += splitPathType[i];
					if (!groups.Contains(groupName))
					{
						list.Add(new SearchTreeGroupEntry(new GUIContent(splitPathType[i]), i + 1));
						groups.Add(groupName);
					}

					groupName += "/";
				}

				var type = new SearchTreeEntry(new GUIContent(splitPathType.Last()))
				{
					level = splitPathType.Length,
					userData = srActionFactory.InstantiateBuild(pathType)
				};
				list.Add(type);
			}

			return list;
		}
	}
}