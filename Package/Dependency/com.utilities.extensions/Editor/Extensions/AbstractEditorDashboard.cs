// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Utilities.Extensions.Editor
{
    public abstract class AbstractEditorDashboard : EditorWindow
    {
        #region Constants

        protected const int TabWidth = 18;
        protected const int EndWidth = 10;
        protected const int MaxCharacterLength = 5000;
        protected const int InnerLabelIndentLevel = 13;

        protected const float InnerLabelWidth = 1.9f;
        protected const float WideColumnWidth = 128f;
        protected const float DefaultColumnWidth = 96f;
        protected const float SettingsLabelWidth = 1.56f;

        #endregion Constants

        #region Default GUIContent

        protected static readonly GUIContent ResetContent = new GUIContent("Reset");
        protected static readonly GUIContent DeleteContent = new GUIContent("Delete");
        protected static readonly GUIContent RefreshContent = new GUIContent("Refresh");
        protected static readonly GUIContent DownloadContent = new GUIContent("Download");
        protected static readonly GUIContent SaveDirectoryContent = new GUIContent("Save Directory");
        protected static readonly GUIContent ChangeDirectoryContent = new GUIContent("Change Save Directory");

        #endregion Default GUIContent

        #region Default GUILayoutOptions

        protected static readonly GUILayoutOption[] DefaultColumnWidthOption =
        {
            GUILayout.Width(DefaultColumnWidth)
        };

        protected static readonly GUILayoutOption[] WideColumnWidthOption =
        {
            GUILayout.Width(WideColumnWidth)
        };

        protected static readonly GUILayoutOption[] ExpandWidthOption =
        {
            GUILayout.ExpandWidth(true)
        };

        #endregion Default GUILayoutOptions

        #region Default GUIStyles

        private static GUIStyle boldCenteredHeaderStyle;

        protected static GUIStyle BoldCenteredHeaderStyle
        {
            get
            {
                if (boldCenteredHeaderStyle == null)
                {
                    var editorStyle = EditorGUIUtility.isProSkin ? EditorStyles.whiteLargeLabel : EditorStyles.largeLabel;

                    if (editorStyle != null)
                    {
                        boldCenteredHeaderStyle = new GUIStyle(editorStyle)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 18,
                            padding = new RectOffset(0, 0, -8, -8)
                        };
                    }
                }

                return boldCenteredHeaderStyle;
            }
        }

        #endregion Default GUIStyles

        protected static string DefaultSaveDirectory => Application.dataPath;

        protected event Action<int> OnTabSelected;

        #region Abstract Implementations

        protected abstract GUIContent DashboardTitleContent { get; }

        protected abstract string DefaultSaveDirectoryKey { get; }

        protected abstract string EditorDownloadDirectory { get; set; }

        protected abstract string[] DashboardTabs { get; }

        protected abstract bool TryCheckDashboardConfiguration(out string errorMessage);

        protected abstract void RenderTab(int tab);

        #endregion Abstract Implementations

        [SerializeField]
        private int tab;

        private Vector2 scrollPosition = Vector2.zero;

        #region Unity Events

        protected virtual void OnEnable()
        {
            titleContent = DashboardTitleContent;
            minSize = new Vector2(WideColumnWidth * 4.375F, WideColumnWidth * 4);
        }

        protected virtual void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(TabWidth);
            var canDrawBody = TryDrawDashboardHeader();
            GUILayout.Space(EndWidth);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayoutExtensions.Divider();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, ExpandWidthOption);
            EditorGUI.indentLevel++;

            if (canDrawBody)
            {
                RenderTab(tab);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #endregion Unity Events

        private bool TryDrawDashboardHeader()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(DashboardTitleContent, BoldCenteredHeaderStyle);
            EditorGUILayout.Space();

            if (!TryCheckDashboardConfiguration(out var errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
                EditorGUILayout.EndVertical();
                return false;
            }

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            tab = GUILayout.Toolbar(tab, DashboardTabs, ExpandWidthOption);

            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl(null);
                OnTabSelected?.Invoke(tab);
            }

            EditorGUILayout.LabelField(SaveDirectoryContent);

            if (string.IsNullOrWhiteSpace(EditorDownloadDirectory))
            {
                EditorDownloadDirectory = EditorPrefs.GetString(DefaultSaveDirectoryKey, DefaultSaveDirectory);
            }

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.TextField(EditorDownloadDirectory, ExpandWidthOption);

                if (GUILayout.Button(ResetContent, WideColumnWidthOption))
                {
                    EditorDownloadDirectory = DefaultSaveDirectory;
                    EditorPrefs.SetString(DefaultSaveDirectoryKey, EditorDownloadDirectory);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(ChangeDirectoryContent, ExpandWidthOption))
                {
                    EditorApplication.delayCall += () =>
                    {
                        var result = EditorUtility.OpenFolderPanel(SaveDirectoryContent.text, EditorDownloadDirectory, string.Empty);

                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            EditorDownloadDirectory = result;
                            EditorPrefs.SetString(DefaultSaveDirectoryKey, EditorDownloadDirectory);
                        }
                    };
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }

        #region Utilities

        protected static string GetLocalPath(string path)
            => path.Replace("\\", "/").Replace(Application.dataPath, "Assets");

        #endregion Utilities
    }
}
