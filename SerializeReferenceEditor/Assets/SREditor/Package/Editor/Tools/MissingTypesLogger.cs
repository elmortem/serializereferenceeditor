using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Text;
using Object = UnityEngine.Object;
using SerializeReferenceEditor.Editor.Settings;

namespace SerializeReferenceEditor.Editor.Tools
{
    public static class MissingTypesLogger
    {
        [MenuItem("Tools/SREditor/Log MissingTypes")] 
        public static void LogMissingTypes()
        {
			var missingAssetsCount = 0;
			var missingTypesCount = 0;
            var stringBuilder = new StringBuilder();
            try
            {
                var editorSettings = SREditorSettings.GetOrCreateSettings();
                var assetSearchFilter = editorSettings != null && !string.IsNullOrWhiteSpace(editorSettings.MissingTypesAssetFilter)
                    ? editorSettings.MissingTypesAssetFilter
                    : "t:Object";

                string[] allGuids = AssetDatabase.FindAssets(assetSearchFilter, new[] { "Assets" });
                for (int index = 0; index < allGuids.Length; index++)
                {
                    string guid = allGuids[index];
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (!(path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                          || path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    Object[] assetObjects = AssetDatabase.LoadAllAssetsAtPath(path);
                    if (assetObjects == null || assetObjects.Length == 0)
                        continue;

                    foreach (var assetObject in assetObjects)
                    {
                        if (assetObject == null)
                            continue;

                        try
                        {
                            if (assetObject is GameObject go)
                            {
                                foreach (var component in go.GetComponentsInChildren<MonoBehaviour>(true))
                                {
                                    if (component == null) continue;
                                    if (SerializationUtility.HasManagedReferencesWithMissingTypes(component))
                                    {
                                        var missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(component);
                                        if (missingTypes != null && missingTypes.Length > 0)
                                        {
											missingAssetsCount++;
											missingTypesCount += missingTypes.Length;

                                            stringBuilder.AppendFormat("Object \"{0}\" (Type: {1}, Instance: {2})",
                                                    component.name,
                                                    component.GetType().FullName,
                                                    component.GetInstanceID())
                                                .AppendLine();

                                            foreach (var missingType in missingTypes)
                                            {
                                                stringBuilder
                                                    .Append('\t')
                                                    .AppendFormat("{0} - {1}.{2}, {3}",
                                                        missingType.referenceId,
                                                        missingType.namespaceName,
                                                        missingType.className,
                                                        missingType.assemblyName);

                                                if (missingType.serializedData != null && missingType.serializedData.Length > 0)
                                                {
                                                    stringBuilder
                                                        .Append('\t')
                                                        .AppendFormat("\n\t\t{0}", missingType.serializedData);
                                                }

                                                stringBuilder.AppendLine();
                                            }
                                        }
                                    }
                                }
                            }
                            else if (assetObject is ScriptableObject scriptable)
                            {
                                if (SerializationUtility.HasManagedReferencesWithMissingTypes(scriptable))
                                {
                                    var missingTypes = SerializationUtility.GetManagedReferencesWithMissingTypes(scriptable);
                                    if (missingTypes != null && missingTypes.Length > 0)
                                    {
										missingAssetsCount++;
										missingTypesCount += missingTypes.Length;

										stringBuilder.AppendFormat("Object \"{0}\" (Type: {1}, Instance: {2})",
                                                scriptable.name,
                                                scriptable.GetType().FullName,
                                                scriptable.GetInstanceID())
                                            .AppendLine();

                                        foreach (var missingType in missingTypes)
                                        {
                                            stringBuilder
                                                .Append('\t')
                                                .AppendFormat("{0} - {1}.{2}, {3}",
                                                    missingType.referenceId,
                                                    missingType.namespaceName,
                                                    missingType.className,
                                                    missingType.assemblyName);

                                            if (missingType.serializedData != null && missingType.serializedData.Length > 0)
                                            {
                                                stringBuilder
                                                    .Append('\t')
                                                    .AppendFormat("\n\t\t{0}", missingType.serializedData);
                                            }

                                            stringBuilder.AppendLine();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"SRMissingTypesLogger: Failed to inspect '{path}' object '{assetObject?.name}'. Exception: {ex.Message}");
                        }
                    }
                }
            }
            finally
            {
                if (stringBuilder.Length > 0)
                {
                    Debug.Log($"Found {missingAssetsCount} assets with {missingTypesCount} missing types:\n" + stringBuilder.ToString());
                }
                else
                {
                    Debug.Log("Not found missing types");
                }
            }
        }
    }
}
