using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using SerializeReferenceEditor.Editor.Settings;

namespace SerializeReferenceEditor.Editor.Tools
{
    public static class MissingTypesCleaner
    {
        [MenuItem("Tools/SREditor/Clean MissingTypes")] 
        public static void CleanMissingTypes()
            => CleanMissingTypesInternal(forceReserialize: false);

        [MenuItem("Tools/SREditor/Clean MissingTypes (Force Reserialize)")] 
        public static void CleanMissingTypesAndReserialize()
            => CleanMissingTypesInternal(forceReserialize: true);

        private static void CleanMissingTypesInternal(bool forceReserialize)
        {
            var settings = SREditorSettings.GetOrCreateSettings();
            var assetSearchFilter = settings != null && !string.IsNullOrWhiteSpace(settings.MissingTypesAssetFilter)
                ? settings.MissingTypesAssetFilter
                : "t:Object";

            string[] assetGuids = AssetDatabase.FindAssets(assetSearchFilter, new[] { "Assets" });
            int totalAssets = assetGuids.Length;
            int cleanedAssetsCount = 0;

            try
            {
                for (int index = 0; index < assetGuids.Length; index++)
                {
                    float progress = (float)(index + 1) / totalAssets;
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Cleaning Missing Types",
                            $"Processing {index + 1}/{totalAssets}", progress))
                    {
                        break;
                    }

                    string guid = assetGuids[index];
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (!(assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                          || assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                          || assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    try
                    {
                        bool cleaned = assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                            ? CleanScene(assetPath, forceReserialize)
                            : CleanAssetAtPath(assetPath, forceReserialize);

                        if (cleaned)
                            cleanedAssetsCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"MissingTypesCleaner: Failed to clean '{assetPath}'. Exception: {ex.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Missing Types Cleaner",
                    $"Processed {totalAssets} assets. Cleaned {cleanedAssetsCount} assets.", "OK");
            }
        }

        private static bool CleanScene(string scenePath, bool forceReserialize)
        {
            Scene scene;
            bool shouldClose = false;
            try
            {
                var existingScene = EditorSceneManager.GetSceneByPath(scenePath);
                bool wasLoaded = existingScene.IsValid() && existingScene.isLoaded;
                scene = wasLoaded ? existingScene : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                shouldClose = !wasLoaded;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"MissingTypesCleaner: Can't open scene '{scenePath}'. Skipped. Reason: {ex.Message}");
                return false;
            }

            bool changed = false;

            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (component == null) continue;

                        try
                        {
                            if (SerializationUtility.HasManagedReferencesWithMissingTypes(component))
                            {
                                SerializationUtility.ClearAllManagedReferencesWithMissingTypes(component);
                                changed = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"MissingTypesCleaner: Failed to process component '{component?.GetType()?.FullName}' in scene '{scenePath}'. {ex.Message}");
                        }
                    }
                }

                if (changed)
                {
                    EditorSceneManager.SaveScene(scene);

                    if (forceReserialize)
                    {
                        AssetDatabase.ForceReserializeAssets(new[] { scenePath },
                            ForceReserializeAssetsOptions.ReserializeAssets);
                    }
                }

                return changed;
            }
            finally
            {
                if (shouldClose)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool CleanAssetAtPath(string assetPath, bool forceReserialize)
        {
            Object[] assetObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assetObjects == null || assetObjects.Length == 0)
                return false;

            bool anyChanged = false;
            GameObject prefabRoot = null;

            foreach (var assetObject in assetObjects)
            {
                if (assetObject == null)
                    continue;

                bool objectChanged = false;

                if (assetObject is GameObject gameObject)
                {
                    prefabRoot ??= gameObject;

                    foreach (var component in gameObject.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (component == null) continue;
                        try
                        {
                            if (SerializationUtility.HasManagedReferencesWithMissingTypes(component))
                            {
                                SerializationUtility.ClearAllManagedReferencesWithMissingTypes(component);
                                objectChanged = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"MissingTypesCleaner: Failed to process component '{component?.GetType()?.FullName}' in asset '{assetPath}'. {ex.Message}");
                        }
                    }
                }
                else if (assetObject is ScriptableObject scriptableObject)
                {
                    try
                    {
                        if (SerializationUtility.HasManagedReferencesWithMissingTypes(scriptableObject))
                        {
                            SerializationUtility.ClearAllManagedReferencesWithMissingTypes(scriptableObject);
                            objectChanged = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"MissingTypesCleaner: Failed to process ScriptableObject '{scriptableObject?.name}' in asset '{assetPath}'. {ex.Message}");
                    }
                }

                if (objectChanged)
                {
                    anyChanged = true;
                    EditorUtility.SetDirty(assetObject);
                }
            }

            if (anyChanged)
            {
                if (prefabRoot != null && PrefabUtility.IsPartOfPrefabAsset(prefabRoot))
                {
                    PrefabUtility.SavePrefabAsset(prefabRoot);
                }
                else
                {
                    AssetDatabase.SaveAssets();
                }

                if (forceReserialize)
                {
                    AssetDatabase.ForceReserializeAssets(new[] { assetPath },
                        ForceReserializeAssetsOptions.ReserializeAssets);
                }
            }

            return anyChanged;
        }
    }
}
