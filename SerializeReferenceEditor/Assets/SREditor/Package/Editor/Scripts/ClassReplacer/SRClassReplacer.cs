using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;

namespace SerializeReferenceEditor.Editor.ClassReplacer
{
	public class SRClassReplacer : EditorWindow
	{
		private string _oldTypeFullName;
		private string _newTypeFullName;
		private Vector2 _scrollPosition;
		private string _statusMessage = "";

		[MenuItem("Tools/SREditor/Class Replacer")]
		public static void ShowWindow()
		{
			GetWindow<SRClassReplacer>("SR Class Replacer");
		}

		private void OnGUI()
		{
			GUILayout.Label("SerializeReference Class Replacer", EditorStyles.boldLabel);

			using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
			{
				_scrollPosition = scrollView.scrollPosition;

				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField("Old Type", GUILayout.Width(100));
					_oldTypeFullName = EditorGUILayout.TextField(_oldTypeFullName);
					if (GUILayout.Button("Select", GUILayout.Width(60)))
					{
						ShowTypeSelector((selectedType) => _oldTypeFullName = GetTypeFullName(selectedType));
					}
				}

				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField("New Type", GUILayout.Width(100));
					_newTypeFullName = EditorGUILayout.TextField(_newTypeFullName);
					if (GUILayout.Button("Select", GUILayout.Width(60)))
					{
						ShowTypeSelector((selectedType) => _newTypeFullName = GetTypeFullName(selectedType));
					}
				}

				if (GUILayout.Button("Replace"))
				{
					if (string.IsNullOrEmpty(_oldTypeFullName) || string.IsNullOrEmpty(_newTypeFullName))
					{
						_statusMessage = "Error: Please select both types";
						return;
					}

					if (EditorUtility.DisplayDialog("Confirm Replace", 
						$"Replace all instances of {_oldTypeFullName} with {_newTypeFullName}?", 
						"Yes", "No"))
					{
						ReplaceInAllAssets(_oldTypeFullName, _newTypeFullName);
					}
				}

				if (!string.IsNullOrEmpty(_statusMessage))
				{
					EditorGUILayout.Space();
					EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
				}
			}
		}

		private bool IsTypeSerializable(Type type)
		{
			if (type == null)
				return false;
			if (!type.IsClass || type.IsAbstract || type.IsInterface)
				return false;
			if (type.Name.Contains("<>"))
				return false;
			if (type.IsGenericType || type.ContainsGenericParameters)
				return false;
			if (type.Namespace?.StartsWith("System") == true || type.IsSubclassOf(typeof(ScriptableObject)))
				return false;
			return type.GetCustomAttributes(typeof(SerializableAttribute), false).Length > 0;
		}

		private void ShowTypeSelector(Action<Type> onTypeSelected)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var types = assemblies
				.SelectMany(a => 
				{
					try 
					{
						return a.GetTypes();
					}
					catch (ReflectionTypeLoadException)
					{
						return Array.Empty<Type>();
					}
				})
				.Where(IsTypeSerializable)
				.OrderBy(t => t.FullName)
				.ToArray();
			
			var searchWindow = ScriptableObject.CreateInstance<TypeSelectorWindow>();
			searchWindow.Init(types, onTypeSelected);
			SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), searchWindow);
		}

		private class TypeSelectorWindow : ScriptableObject, ISearchWindowProvider
		{
			private Type[] _types;
			private Action<Type> _onTypeSelected;

			public void Init(Type[] types, Action<Type> onTypeSelected)
			{
				_types = types;
				_onTypeSelected = onTypeSelected;
			}

			public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
			{
				var list = new List<SearchTreeEntry>
				{
					new SearchTreeGroupEntry(new GUIContent("Select Type"))
				};

				var groups = new HashSet<string>();

				foreach (var type in _types)
				{
					var path = type.FullName?.Split('.');
					if (path == null) continue;

					var groupPath = "";
					for (int i = 0; i < path.Length - 1; i++)
					{
						groupPath += path[i];
						if (!groups.Contains(groupPath))
						{
							list.Add(new SearchTreeGroupEntry(new GUIContent(path[i]), i + 1));
							groups.Add(groupPath);
						}
						groupPath += "/";
					}

					list.Add(new SearchTreeEntry(new GUIContent(path[^1]))
					{
						level = path.Length,
						userData = type
					});
				}

				return list;
			}

			public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
			{
				if (entry.userData is Type selectedType)
				{
					_onTypeSelected?.Invoke(selectedType);
					return true;
				}
				return false;
			}
		}

		private string GetTypeFullName(Type type)
		{
			var assemblyName = type.Assembly.GetName().Name;
			if (assemblyName.StartsWith("Assembly-CSharp"))
			{
				assemblyName = "Assembly-CSharp";
			}
			return $"{assemblyName}, {type.FullName}";
		}

		private void ReplaceInAllAssets(string oldTypeFullName, string newTypeFullName)
		{
			string[] guids = AssetDatabase.FindAssets("t:Object");
			int processedCount = 0;
			
			try
			{
				AssetDatabase.StartAssetEditing();
				
				foreach (string guid in guids)
				{
					string path = AssetDatabase.GUIDToAssetPath(guid);
					if (path.EndsWith(".prefab") || path.EndsWith(".unity") || path.EndsWith(".asset"))
					{
						if (ProcessAsset(path, oldTypeFullName, newTypeFullName))
						{
							processedCount++;
						}
					}
				}
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
				AssetDatabase.Refresh();
				_statusMessage = $"Processed {processedCount} files with changes.";
				Repaint();
			}
		}

		private bool ProcessAsset(string path, string oldTypeFullName, string newTypeFullName)
		{
			return TypeReplacer.ReplaceTypeInFile(path, oldTypeFullName, newTypeFullName);
		}
	}
}
