using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.ClassReplacer
{
    [InitializeOnLoad]
    public class SRTypeUpgrader : AssetPostprocessor
    {
        private static Dictionary<(string assembly, string type), Type> _typeReplacements = new();
        private static HashSet<string> _processedPrefabs = new HashSet<string>();

        static SRTypeUpgrader()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                CollectTypeReplacementsForAssembly(assembly.Location);
            }

            CompilationPipeline.assemblyCompilationFinished += (assemblyPath, messages) =>
            {
                CollectTypeReplacementsForAssembly(assemblyPath);
            };

            EditorSceneManager.sceneSaving += OnSceneSaving;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            if (Selection.activeObject != null)
            {
                ProcessObject(Selection.activeObject);
            }
        }

        private static void ProcessObject(Object obj)
        {
            if (obj == null) return;

            var assetPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath)) return;

            if (PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                if (_processedPrefabs.Contains(assetPath)) return;
                _processedPrefabs.Add(assetPath);
            }

            bool modified = false;
            foreach (var replacement in _typeReplacements)
            {
                var (oldAssembly, oldType) = replacement.Key;
                var newType = replacement.Value;

                var oldTypePattern = string.IsNullOrEmpty(oldAssembly) ? oldType : $"{oldAssembly}, {oldType}";
                var newAssembly = newType.Assembly.GetName().Name;
                var newTypePattern = string.IsNullOrEmpty(newAssembly) ? newType.FullName : $"{newAssembly}, {newType.FullName}";

                if (TypeReplacer.ReplaceTypeInFile(assetPath, oldTypePattern, newTypePattern))
                {
                    modified = true;
                }
            }

            if (modified)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                EditorUtility.SetDirty(obj);
            }
        }

        private static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            foreach (var rootObj in scene.GetRootGameObjects())
            {
                foreach (var component in rootObj.GetComponentsInChildren<Component>(true))
                {
                    if (component != null)
                    {
                        ProcessObject(component);
                    }
                }
            }
        }

        private static void CollectTypeReplacementsForAssembly(string assemblyPath)
        {
            var assemblyName = System.IO.Path.GetFileNameWithoutExtension(assemblyPath);
            
            _typeReplacements = _typeReplacements
                .Where(kvp => kvp.Key.assembly != assemblyName)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);
            if (assembly == null) return;

            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.GetCustomAttributes<FormerlySerializedTypeAttribute>().Any());

                foreach (var type in types)
                {
                    var attributes = type.GetCustomAttributes<FormerlySerializedTypeAttribute>();
                    foreach (var attr in attributes)
                    {
                        var key = (attr.OldAssemblyName, $"{attr.OldNamespace}.{attr.OldTypeName}".TrimStart('.'));
                        _typeReplacements[key] = type;
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
            }
        }

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var assetPath in importedAssets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset != null)
                {
                    ProcessObject(asset);
                }
            }
        }
    }
}
