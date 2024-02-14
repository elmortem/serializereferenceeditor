using System;
using System.Collections.Generic;
using System.Linq;
using SerializeReferenceEditor.Editor.SRActions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.Drawers
{
    public class SRTypesContainer : ScriptableObject, ISearchWindowProvider
    {
        private List<SearchTreeEntry> _types = new();

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
            => _types;

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is BaseSRAction action)
                action.Apply();
            return true;
        }

        public static SRTypesContainer MakeTypesContainer(
            SerializedProperty currentProperty,
            SerializedProperty parentProperty,
            List<(string, BaseSRAction)> valueTuples)
        {
            var typesContainer = ScriptableObject.CreateInstance<SRTypesContainer>();
            typesContainer._types = GenerateSearchTreeEntries(currentProperty, parentProperty, valueTuples);
            return typesContainer;
        }

        private static List<SearchTreeEntry> GenerateSearchTreeEntries(SerializedProperty currentProperty,
            SerializedProperty parentProperty,
            List<(string, BaseSRAction)> valueTuples)
        {
            valueTuples.Sort(SortTypesRule);
            var list = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Elements")),
                new(new GUIContent("Erase"))
                {
                    userData = new ErasePropertySRAction(currentProperty, parentProperty),
                    level = 1
                },
            };
            list.AddRange(AttachTypes(valueTuples));

            return list;
        }

        private static List<SearchTreeEntry> AttachTypes(List<(string, BaseSRAction)> valueTuples)
        {
            List<SearchTreeEntry> list = new();
            var groups = new List<string>();
            foreach (var valueTuple in valueTuples)
            {
                var entryType = valueTuple.Item1.Split('/');
                var groupName = "";
                for (int i = 0; i < entryType.Length - 1; i++)
                {
                    groupName += entryType[i];
                    if (!groups.Contains(groupName))
                    {
                        list.Add(new SearchTreeGroupEntry(new GUIContent(entryType[i]), i + 1));
                        groups.Add(groupName);
                    }

                    groupName += "/";
                }

                var type = new SearchTreeEntry(new GUIContent(entryType.Last()))
                {
                    level = entryType.Length,
                    userData = valueTuple.Item2
                };
                list.Add(type);
            }

            return list;
        }

        private static int SortTypesRule((string, BaseSRAction) typeA, (string, BaseSRAction) typeB)
        {
            var pathTypeA = typeA.Item1.Split('/');
            var pathTypeB = typeB.Item1.Split('/');

            for (int i = 0; i < pathTypeA.Length; i++)
            {
                if (i > pathTypeB.Length)
                    return 1;

                var compare = string.Compare(pathTypeA[i], pathTypeB[i], StringComparison.Ordinal);
                if (compare == 0)
                    continue;

                if (pathTypeA.Length != pathTypeB.Length
                    && (i == pathTypeA.Length - 1
                        || i == pathTypeB.Length - 1))
                    return pathTypeA.Length < pathTypeB.Length ? 1 : -1;

                return compare;
            }

            return 0;
        }
    }
}