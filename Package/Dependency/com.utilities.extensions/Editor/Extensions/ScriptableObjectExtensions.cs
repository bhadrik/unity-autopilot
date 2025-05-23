// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Utilities.Extensions.Editor
{
    /// <summary>
    /// Extensions for <see cref="ScriptableObject"/>s
    /// </summary>
    public static class ScriptableObjectExtensions
    {
        /// <summary>
        /// Creates, saves, and then optionally selects a new asset for the target <see cref="ScriptableObject"/>.
        /// </summary>
        /// <param name="scriptableObject"><see cref="ScriptableObject"/> you want to create an asset file for.</param>
        /// <param name="ping">The new asset should be selected and opened in the inspector.</param>
        public static T CreateAsset<T>(this T scriptableObject, bool ping = true) where T : ScriptableObject
            => CreateAsset(scriptableObject, null, ping);

        /// <summary>
        /// Creates, saves, and then opens a new asset for the target <see cref="ScriptableObject"/>.
        /// </summary>
        /// <param name="scriptableObject"><see cref="ScriptableObject"/> you want to create an asset file for.</param>
        /// <param name="path">Optional path for the new asset.</param>
        /// <param name="ping">The new asset should be selected and opened in the inspector.</param>
        public static T CreateAsset<T>(this T scriptableObject, string path, bool ping = true) where T : ScriptableObject
            => CreateAsset(scriptableObject, path, null, ping, true);

        /// <summary>
        /// Creates, saves, and then opens a new asset for the target <see cref="ScriptableObject"/>.
        /// </summary>
        /// <param name="scriptableObject"><see cref="ScriptableObject"/> you want to create an asset file for.</param>
        /// <param name="path">Optional path for the new asset.</param>
        /// <param name="fileName">Optional filename for the new asset.</param>
        /// <param name="ping">The new asset should be selected and opened in the inspector.</param>
        /// <param name="unique">Is the new asset unique, or can we make copies?</param>
        public static T CreateAsset<T>(this T scriptableObject, string path, string fileName, bool ping, bool unique = true) where T : ScriptableObject
        {
            const string assetExt = ".asset";
            const string resources = "Resources";

            var name = string.IsNullOrEmpty(fileName)
                ? scriptableObject.GetType().Name
                : fileName;
            name = name.Replace(" ", string.Empty);

            if (string.IsNullOrWhiteSpace(path))
            {
                var defaultPath = $"{Application.dataPath}/{resources}".ToForward();

                if (!Directory.Exists(defaultPath))
                {
                    Directory.CreateDirectory(defaultPath);
                }

                path = EditorUtility.SaveFolderPanel(
                    $"Create new {typeof(T).Name}",
                    defaultPath,
                    string.Empty);

                if (string.IsNullOrWhiteSpace(path))
                {
                    path = defaultPath;
                }
            }

            path = path.Replace(assetExt, string.Empty);

            if (!string.IsNullOrWhiteSpace(Path.GetExtension(path)))
            {
                var subtractedPath = path[path.LastIndexOf(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)..];
                path = path.Replace(subtractedPath, string.Empty);
            }

            var longPath = $"{Directory.GetParent(Application.dataPath)!.FullName}/".ToForward();
            path = path.Replace(longPath, string.Empty);

            if (!Directory.Exists(Path.GetFullPath(path)))
            {
                Directory.CreateDirectory(Path.GetFullPath(path));
            }

            // uses forward on purpose bc Unity likes it that way.
            path = $"{path}/{name}{assetExt}";

            if (unique)
            {
                path = AssetDatabase.GenerateUniqueAssetPath(path);
            }

            if (File.Exists(Path.GetFullPath(path)))
            {
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }

            AssetDatabase.CreateAsset(scriptableObject, path);
            AssetDatabase.SaveAssets();

            if (!EditorApplication.isUpdating)
            {
                AssetDatabase.Refresh();
            }

            scriptableObject = AssetDatabase.LoadAssetAtPath<T>(path);

            if (ping)
            {
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(scriptableObject);
                    Selection.activeObject = scriptableObject;
                };
            }

            Debug.Assert(scriptableObject.IsNotNull());
            return scriptableObject;
        }

        private static string ToForward(this string @string)
        {
            const string backward = "\\";
            const string forward = "/";
            return @string.Replace(backward, forward);
        }

        /// <summary>
        /// Attempts to find the asset associated to the instance of the <see cref="ScriptableObject"/>,
        /// if none is found a new asset is created.
        /// </summary>
        /// <param name="scriptableObject"><see cref="ScriptableObject"/> you want to create an asset file for.</param>
        /// <param name="ping">The new asset should be selected and opened in the inspector.</param>
        public static T GetOrCreateAsset<T>(this T scriptableObject, bool ping = true) where T : ScriptableObject
            => GetOrCreateAsset(scriptableObject, null, ping);

        /// <summary>
        /// Attempts to find the asset associated to the instance of the <see cref="ScriptableObject"/>,
        /// if none is found a new asset is created.
        /// </summary>
        /// <param name="scriptableObject"><see cref="ScriptableObject"/> you want to create an asset file for.</param>
        /// <param name="path">Optional path for the new asset.</param>
        /// <param name="ping">The new asset should be selected and opened in the inspector.</param>
        public static T GetOrCreateAsset<T>(this T scriptableObject, string path, bool ping = true) where T : ScriptableObject
            => GetOrCreateAsset(scriptableObject, path, null, ping);

        /// <summary>
        /// Attempts to find the asset associated to the instance of the <see cref="ScriptableObject"/>,
        /// if none is found a new asset is created.
        /// </summary>
        /// <param name="scriptableObject"><see cref="ScriptableObject"/> you want get or create an asset file for.</param>
        /// <param name="path">Optional path for the new asset.</param>
        /// <param name="fileName">Optional filename for the new asset.</param>
        /// <param name="ping">The new asset should be selected and opened in the inspector.</param>
        public static T GetOrCreateAsset<T>(this T scriptableObject, string path, string fileName, bool ping) where T : ScriptableObject
            => AssetDatabase.TryGetGUIDAndLocalFileIdentifier(scriptableObject, out var guid, out long _)
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))
                : scriptableObject.CreateAsset(path, fileName, ping, false);

        /// <summary>
        /// Gets all the scriptable object instances in the project.
        /// </summary>
        /// <typeparam name="T">The Type of <see cref="ScriptableObject"/> you're wanting to find instances of.</typeparam>
        /// <returns>An Array of instances for the type.</returns>
        public static T[] GetAllInstances<T>() where T : ScriptableObject
        {
            // FindAssets uses tags check documentation for more info
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var instances = new List<T>();

            for (var i = 0; i < guids.Length; i++)
            {
                var instance = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[i]));

                if (instance.IsNotNull())
                {
                    instances.Add(instance);
                }
            }

            return instances.ToArray();
        }
    }
}
