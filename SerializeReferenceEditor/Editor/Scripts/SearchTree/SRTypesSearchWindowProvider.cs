using System.Collections.Generic;
using SerializeReferenceEditor.Editor.SRActions;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace SerializeReferenceEditor.Editor.Drawers
{
    public class SRTypesSearchWindowProvider : ScriptableObject, ISearchWindowProvider
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

        public static SRTypesSearchWindowProvider MakeTypesContainer(
            SRActionFactory srActionFactory,
            SRTypeTreeFactory srTypeTreeFactory)
        {
            var typesContainer = ScriptableObject.CreateInstance<SRTypesSearchWindowProvider>();
            typesContainer._types = GenerateSearchTreeEntries(srActionFactory, srTypeTreeFactory);
            return typesContainer;
        }

        private static List<SearchTreeEntry> GenerateSearchTreeEntries(
            SRActionFactory srActionFactory,
            SRTypeTreeFactory srTypeTreeFactory)
        {
            var list = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Elements")),
                new(new GUIContent("Erase"))
                {
                    userData = srActionFactory.EraseBuild(),
                    level = 1
                },
            };
            list.AddRange(srTypeTreeFactory.MakeTypesTree(srActionFactory));

            return list;
        }
    }
}