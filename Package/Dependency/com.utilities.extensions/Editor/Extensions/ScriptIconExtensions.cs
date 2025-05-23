// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Utilities.Extensions.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DefaultAsset))]
    public class IconEditor : UnityEditor.Editor
    {
        private Texture2D icon;
        private string filter;
        private string[] filters;
        private bool filterFlag;
        private bool overwriteIcons;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();

            GUI.enabled = true;
            icon = (Texture2D)EditorGUILayout.ObjectField("Icon Texture", icon, typeof(Texture2D), false);
            filter = EditorGUILayout.TextField(new GUIContent("Partial name filters", "Use comma separated values for each partial name search."), filter);
            filterFlag = EditorGUILayout.Toggle(filterFlag ? "Skipping filter results" : "Targeting filter results", filterFlag);

            EditorGUI.BeginChangeCheck();
            overwriteIcons = EditorGUILayout.Toggle("Overwrite Icon?", overwriteIcons);
            EditorGUILayout.Space();

            if (GUILayout.Button("Set Icons for child script assets"))
            {
                filters = !string.IsNullOrEmpty(filter) ? filter.Split(',') : null;
                var selectedAsset = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);

                for (var i = 0; i < selectedAsset.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Updating Icons...", $"{i} of {selectedAsset.Length} {selectedAsset[i].name}", i / (float)selectedAsset.Length);
                    var path = AssetDatabase.GetAssetPath(selectedAsset[i]);

                    if (!path.Contains(".cs")) { continue; }

                    if (filters != null)
                    {
                        var matched = filterFlag;

                        foreach (var asset in filters)
                        {
                            if (selectedAsset[i].name.ToLower().Contains(asset.ToLower()))
                            {
                                matched = !filterFlag;
                            }
                        }

                        if (overwriteIcons && !matched ||
                           !overwriteIcons && matched)
                        {
                            continue;
                        }
                    }

                    try
                    {
                        SetIcon(path, icon, overwriteIcons);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                EditorUtility.ClearProgressBar();
            }

            GUI.enabled = false;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
        }

        private static void SetIcon(string objectPath, Texture2D texture, bool overwrite)
        {
            if (AssetImporter.GetAtPath(objectPath) is not MonoImporter monoImporter) { return; }

            var setIcon = monoImporter.GetIcon();

            if (setIcon.IsNotNull() && !overwrite) { return; }

            monoImporter.SetIcon(texture);
            monoImporter.SaveAndReimport();
        }
    }
}
