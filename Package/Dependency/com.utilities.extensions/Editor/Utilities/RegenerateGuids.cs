// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utilities.Extensions.Editor
{
    /// <summary>
    /// Used to regenerate guids for Unity assets.<br/>
    /// Based on https://gist.github.com/ZimM-LostPolygon/7e2f8a3e5a1be183ac19
    /// </summary>
    public static class GuidRegenerator
    {
        private static readonly string[] unityFileExtensions =
        {
            "*.meta",
            "*.mat",
            "*.anim",
            "*.prefab",
            "*.unity",
            "*.asset",
            "*.guiskin",
            "*.fontsettings",
            "*.controller",
            "*.json"
        };

        /// <summary>
        /// Regenerate the guids for assets located in the <see cref="directory"/>.
        /// </summary>
        /// <param name="directory">The root directory to search for assets to regenerate guids for.</param>
        /// <param name="refreshAssetDatabase">Should <see cref="AssetDatabase.Refresh(ImportAssetOptions)"/> be called after finishing regeneration? (Default is true)</param>
        public static void RegenerateGuids(string directory, bool refreshAssetDatabase = true)
            => RegenerateGuids(refreshAssetDatabase, directory);

        /// <summary>
        /// Regenerate the guids for assets located in the <see cref="directories"/>.
        /// </summary>
        /// <param name="refreshAssetDatabase">Should <see cref="AssetDatabase.Refresh(ImportAssetOptions)"/> be called after finishing regeneration? (Default is true)</param>
        /// <param name="directories">The root directory to search for assets to regenerate guids for.</param>
        public static void RegenerateGuids(bool refreshAssetDatabase = true, params string[] directories)
        {
            try
            {
                AssetDatabase.StartAssetEditing();
                RegenerateGuidsInternal(directories);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();

                if (refreshAssetDatabase)
                {
                    EditorApplication.delayCall += AssetDatabase.Refresh;
                }
            }
        }

        private static void RegenerateGuidsInternal(IEnumerable<string> directories)
        {
            var filesPaths = new List<string>();

            foreach (var directory in directories)
            {
                filesPaths.AddRange(GetUnityAssetsInDirectory(directory));
            }

            // Create dictionary to hold old-to-new GUID map
            var guidOldToNewMap = new Dictionary<string, string>();
            var guidsInFileMap = new Dictionary<string, List<string>>();

            // We must only replace GUIDs for Resources present in the path.
            // Otherwise built-in resources (shader, meshes etc) get overwritten.
            var ownGuids = new HashSet<string>();

            // Traverse all files, remember which GUIDs are in which files and generate new GUIDs
            var counter = 0;

            foreach (var filePath in filesPaths)
            {
                EditorUtility.DisplayProgressBar("Regenerating guids...", filePath, counter / (float)filesPaths.Count);

                var isFirstGuid = true;
                var guids = GetGuids(File.ReadAllText(filePath)).ToList();

                foreach (var oldGuid in guids)
                {
                    // First GUID in .meta file is always the GUID of the asset itself
                    if (isFirstGuid && Path.GetExtension(filePath) == ".meta")
                    {
                        ownGuids.Add(oldGuid);
                        isFirstGuid = false;
                    }

                    // Generate and save new GUID if we haven't added it before
                    if (!guidOldToNewMap.ContainsKey(oldGuid))
                    {
                        var newGuid = Guid.NewGuid().ToString("N");
                        guidOldToNewMap.Add(oldGuid, newGuid);
                    }

                    if (!guidsInFileMap.ContainsKey(filePath))
                    {
                        guidsInFileMap[filePath] = new List<string>();
                    }

                    if (!guidsInFileMap[filePath].Contains(oldGuid))
                    {
                        guidsInFileMap[filePath].Add(oldGuid);
                    }
                }

                counter++;
            }

            // Traverse the files again and replace the old GUIDs
            counter = -1;
            var guidsInFileMapKeysCount = guidsInFileMap.Keys.Count;

            foreach (var filePath in guidsInFileMap.Keys)
            {
                EditorUtility.DisplayProgressBar("Replacing guids...", filePath, counter / (float)guidsInFileMapKeysCount);
                counter++;

                try
                {
                    var contents = File.ReadAllText(filePath);

                    foreach (var oldGuid in guidsInFileMap[filePath])
                    {
                        if (!ownGuids.Contains(oldGuid)) { continue; }

                        var newGuid = guidOldToNewMap[oldGuid];

                        if (!IsValidGuid(newGuid))
                        {
                            throw new Exception($"Invalid guid found for {filePath}!");
                        }

                        contents = contents.Replace($"guid: {oldGuid}", $"guid: {newGuid}");
                    }

                    EnsureAssetIsWritable(filePath);
                    File.WriteAllText(filePath, contents);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to replace guids for {filePath}\n{e}");
                }
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>
        /// Sets a specific guid to the specified asset at path.
        /// </summary>
        /// <param name="assetPath">The asset to set the guid to.</param>
        /// <param name="guid">The guid to assign.</param>
        /// <param name="refreshAssetDatabase">Should <see cref="AssetDatabase.Refresh(ImportAssetOptions)"/> be called after finishing regeneration? (Default is true)</param>
        public static void SetGuidForAssetAtPath(string assetPath, string guid, bool refreshAssetDatabase = true)
        {
            AssetDatabase.StartAssetEditing();
            EditorUtility.DisplayProgressBar("Set guid for asset", $"Setting {guid} -> {assetPath}", -1);

            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                if (asset.IsNull())
                {
                    throw new Exception($"Invalid asset at {assetPath}");
                }

                if (!IsValidGuid(guid))
                {
                    throw new Exception($"Invalid guid {guid}");
                }

                var existingAsset = AssetDatabase.GUIDToAssetPath(guid);

                // Don't set if the guid already matches
                if (existingAsset == assetPath) { return; }

                if (!string.IsNullOrWhiteSpace(existingAsset))
                {
                    throw new Exception($"Guid {guid} already assigned to \"{existingAsset}\"!");
                }

                EnsureAssetIsWritable(assetPath);

                // Set the new GUID
                var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath);
                var contents = File.ReadAllText(metaPath);
                var oldGuid = GetGuids(contents).First();
                contents = contents.Replace($"guid: {oldGuid}", $"guid: {guid}");

                // Save the changes
                File.WriteAllText(metaPath, contents);

                // Now iterate over all assets and replace any occurrence of oldGuid with newGuid
                var allAssetsPaths = GetUnityAssetsInDirectory(Application.dataPath).ToList();
                var counter = 0;

                foreach (var path in allAssetsPaths.Where(path => assetPath != path))
                {
                    EditorUtility.DisplayProgressBar("Replacing guids...", path, ++counter / (float)allAssetsPaths.Count);

                    try
                    {
                        var assetContents = File.ReadAllText(path);

                        if (assetContents.Contains(oldGuid))
                        {
                            assetContents = assetContents.Replace($"guid: {oldGuid}", $"guid: {guid}");
                            File.WriteAllText(path, assetContents);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to replace guid for {path}!\n{e}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();

                if (refreshAssetDatabase)
                {
                    EditorApplication.delayCall += AssetDatabase.Refresh;
                }
            }
        }

        private static IEnumerable<string> GetUnityAssetsInDirectory(string directory)
        {
            var filesPaths = new List<string>();
            directory = Path.GetFullPath(directory);

            foreach (var extension in unityFileExtensions)
            {
                try
                {
                    filesPaths.AddRange(Directory.GetFiles(directory, extension, SearchOption.AllDirectories));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to get assets for {extension} in {directory}!\n{e}");
                }
            }

            return filesPaths;
        }

        private static IEnumerable<string> GetGuids(string text)
        {
            const string guidStart = "guid: ";
            const int guidLength = 32;
            var textLength = text.Length;
            var guidStartLength = guidStart.Length;
            var guids = new List<string>();
            var index = 0;

            while (index + guidStartLength + guidLength < textLength)
            {
                index = text.IndexOf(guidStart, index, StringComparison.Ordinal);

                if (index == -1)
                {
                    break;
                }

                index += guidStartLength;
                var guid = text.Substring(index, guidLength);
                index += guidLength;

                if (IsValidGuid(guid))
                {
                    guids.Add(guid);
                }
            }

            return guids;
        }

        private static bool IsValidGuid(string text)
            => !string.IsNullOrWhiteSpace(text) && text.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'z');

        private static void EnsureAssetIsWritable(string assetPath)
        {
            // Get attribute and make it writable
            var attributes = File.GetAttributes(assetPath);

            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                attributes ^= FileAttributes.ReadOnly;
            }

            File.SetAttributes(assetPath, attributes);
        }
    }
}
