using System;
using System.Collections.Generic;
using SerializeReferenceEditor.Editor.Processing.DoubleCleaner;
using SerializeReferenceEditor.Editor.Processing.TypeReplace;
using SerializeReferenceEditor.Editor.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SerializeReferenceEditor.Editor.Processing
{
    public class ProcessingCoordinator : AssetPostprocessor
    {
        private static readonly HashSet<string> PendingAssetPaths = new();
        private static readonly HashSet<Object> PendingSceneObjects = new();
        private static readonly HashSet<string> InFlightImports = new();

        private static bool _processingScheduled;
        private static bool _isProcessing;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;

            AssetChangeDetector.ChangeEvent -= OnAssetChanged;
            AssetChangeDetector.ChangeEvent += OnAssetChanged;
        }

        private static SREditorSettings Settings => SREditorSettings.GetOrCreateSettings();

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!Settings.FormerlySerializedTypeOnAssetImport)
                return;

            foreach (var path in importedAssets)
            {
                if (string.IsNullOrEmpty(path))
                    continue;
                
                if (InFlightImports.Remove(path))
                    continue;

                EnqueueAssetPath(path);
            }
        }

        private static void OnAssetChanged(Object changedObject)
        {
            if (_isProcessing)
                return;

            if (changedObject == null)
                return;

            var assetPath = AssetDatabase.GetAssetPath(changedObject);
            if (!string.IsNullOrEmpty(assetPath))
            {
                EnqueueAssetPath(assetPath);
            }
            else
            {
                EnqueueSceneObject(changedObject);
            }
        }

        private static void OnSelectionChanged()
        {
            if (!Settings.FormerlySerializedTypeOnAssetSelect)
                return;

            foreach (var obj in Selection.objects)
            {
                if (obj == null)
                    continue;

                var path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path))
                {
                    EnqueueAssetPath(path);
                }
                else
                {
                    EnqueueSceneObject(obj);
                }
            }
        }

        private static void OnSceneSaving(Scene scene, string path)
        {
            if (!Settings.FormerlySerializedTypeOnSceneSave)
                return;

            var roots = scene.GetRootGameObjects();
            bool anyChanged = false;
            foreach (var go in roots)
            {
                bool modified = TypeReplacer.TryUpgradeAsset(string.Empty, go, out bool clearedMissing);
                modified |= SRDuplicateCleaner.TryCleanupObject(go, Settings.DuplicateMode);
                anyChanged |= modified | clearedMissing;
            }

            if (anyChanged)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private static void EnqueueAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (PendingAssetPaths.Add(path))
                ScheduleProcessing();
        }

        private static void EnqueueSceneObject(Object obj)
        {
            if (obj == null)
                return;
            if (PendingSceneObjects.Add(obj))
                ScheduleProcessing();
        }

        private static void ScheduleProcessing()
        {
            if (_processingScheduled)
                return;
            _processingScheduled = true;
            EditorApplication.delayCall += ProcessBatch;
        }

        private static void ProcessBatch()
        {
            _processingScheduled = false;
            if (_isProcessing)
                return;
            _isProcessing = true;

            try
            {
                var paths = new List<string>(PendingAssetPaths);
                PendingAssetPaths.Clear();
                var sceneObjects = new List<Object>(PendingSceneObjects);
                PendingSceneObjects.Clear();

                var reimportPaths = new HashSet<string>();
                var dirtyAssets = new HashSet<Object>();

                foreach (var path in paths)
                {
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);

                    bool modified = TypeReplacer.TryUpgradeAsset(path, asset, out bool clearedMissing);
                    bool cleaned = false;
                    if (asset != null)
                    {
                        cleaned = SRDuplicateCleaner.TryCleanupObject(asset, Settings.DuplicateMode);
                    }

                    if (modified && !string.IsNullOrEmpty(path))
                    {
                        reimportPaths.Add(path);
                    }

                    if (asset != null && (clearedMissing || cleaned))
                    {
                        EditorUtility.SetDirty(asset);
                        dirtyAssets.Add(asset);
                    }
                }

                foreach (var obj in sceneObjects)
                {
                    if (obj == null) continue;

                    string scenePath = string.Empty;
                    if (obj is Component c && c.gameObject.scene.IsValid())
                        scenePath = c.gameObject.scene.path;
                    else if (obj is GameObject go && go.scene.IsValid())
                        scenePath = go.scene.path;

                    bool modified = TypeReplacer.TryUpgradeAsset(string.Empty, obj, out bool clearedMissing);
                    bool cleaned = SRDuplicateCleaner.TryCleanupObject(obj, Settings.DuplicateMode);

                    if ((modified || clearedMissing || cleaned) && !string.IsNullOrEmpty(scenePath))
                    {
                        var scene = SceneManager.GetSceneByPath(scenePath);
                        if (scene.IsValid())
                            EditorSceneManager.MarkSceneDirty(scene);
                    }
                }

                if (reimportPaths.Count > 0)
                {
                    foreach (var p in reimportPaths)
                        InFlightImports.Add(p);

                    foreach (var p in reimportPaths)
                    {
                        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
                    }
                }

                if (dirtyAssets.Count > 0)
                {
                    AssetDatabase.SaveAssets();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SRProcessingCoordinator error: {e}");
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}
