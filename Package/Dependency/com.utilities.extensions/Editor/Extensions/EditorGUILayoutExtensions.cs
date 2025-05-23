// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Utilities.Extensions.Editor
{
    public static class EditorGUILayoutExtensions
    {
        /// <summary>
        /// Draws a divider.
        /// </summary>
        /// <param name="height">Height in pixels</param>
        /// <param name="color">Optional, color.</param>
        public static void Divider(int height = 1, Color? color = null)
        {
            var rect = EditorGUILayout.GetControlRect(false, height);
            rect.height = height;
            color ??= EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.5f, 0.5f, 0.5f, 1);
            EditorGUI.DrawRect(rect, color.Value);
        }

        private static readonly Dictionary<string, Vector2> scrollCache = new Dictionary<string, Vector2>();

        private static MethodInfo scrollableTextAreaInternal;

        private static MethodInfo ScrollableTextAreaInternal
        {
            get
            {
                if (scrollableTextAreaInternal != null) { return scrollableTextAreaInternal; }
                var types = new[]
                {
                    typeof(Rect),
                    typeof(string),
                    typeof(Vector2).MakeByRefType(),
                    typeof(GUIStyle)
                };
                return scrollableTextAreaInternal = typeof(EditorGUI)
                    .GetMethod("ScrollableTextAreaInternal", BindingFlags.Static | BindingFlags.NonPublic, null, types, null);
            }
        }

        /// <summary>
        /// Draws a text area.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="property"></param>
        public static void DrawTextArea(Rect rect, SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                var guid = property.GetUniqueIdentifier();
                scrollCache.TryAdd(guid, Vector2.zero);
                var parameters = new object[]
                {
                    rect,
                    property.stringValue,
                    scrollCache[guid],
                    EditorStyles.textArea
                };
                property.stringValue = (string)ScrollableTextAreaInternal.Invoke(null, parameters);
                scrollCache[guid] = (Vector2)parameters[2];
            }
            else
            {
                EditorGUI.LabelField(rect, $"Use {nameof(DrawTextArea)} with string.");
            }
        }

        private static readonly Dictionary<string, TextAreaHeight> cachedTextAreaHeights = new Dictionary<string, TextAreaHeight>();

        private class TextAreaHeight
        {
            public TextAreaHeight(GUIContent content, int height)
            {
                this.content = content;
                this.height = height;
            }

            public GUIContent content;
            public float height;
        }

        /// <summary>
        /// Returns the height of a <see cref="TextAreaAttribute"/>.
        /// </summary>
        /// <param name="property"><see cref="SerializedProperty"/></param>
        /// <param name="width">The width of property.</param>
        /// <param name="minLines">Optional, default is 1.</param>
        /// <param name="maxLines">Optional, default is 10.</param>
        /// <returns>The height of the text area.</returns>
        public static float GetTextAreaHeight(SerializedProperty property, float width, int minLines = 1, int maxLines = 10)
        {
            property.serializedObject.Update();

            if (string.IsNullOrWhiteSpace(property.stringValue))
            {
                return EditorGUIUtility.singleLineHeight;
            }

            if (minLines < 1)
            {
                minLines = 1;
            }

            const float lineHeight = 13f;
            var guid = property.GetUniqueIdentifier();
            var textContent = new GUIContent(property.stringValue);
            cachedTextAreaHeights.TryAdd(guid, new TextAreaHeight(textContent, 0));
            var cachedContent = cachedTextAreaHeights[guid].content;
            var cachedHeight = cachedTextAreaHeights[guid].height;
            var hasSameTextValue = property.stringValue.Equals(cachedContent.text);
            var textAreaHeight = EditorStyles.textArea.CalcHeight(hasSameTextValue ? cachedContent : textContent, width);
            var baseTextAreaHeight = EditorGUIUtility.singleLineHeight;
            var result = baseTextAreaHeight + ((Mathf.Clamp(Mathf.CeilToInt(textAreaHeight / lineHeight), minLines, maxLines) - 1) * lineHeight);

            if (hasSameTextValue && cachedHeight > 0 && !Mathf.Approximately(result, cachedHeight))
            {
                return cachedHeight;
            }

            cachedTextAreaHeights[guid].content.text = property.stringValue;
            cachedTextAreaHeights[guid].height = result;
            return result;
        }

        /// <summary>
        /// Draws a progress bar.
        /// </summary>
        /// <param name="text">Text label for the progress bar.</param>
        /// <param name="progress">The current value of the progress bar.</param>
        /// <param name="guiLayoutOptions">Optional, <see cref="GUILayoutOption"/>s.</param>
        public static void DrawProgressBar(string text, float progress, params GUILayoutOption[] guiLayoutOptions)
            => DrawProgressBar(new GUIContent(text), progress, guiLayoutOptions);

        /// <summary>
        /// Draws a progress bar.
        /// </summary>
        /// <param name="content">Content label for the progress bar.</param>
        /// <param name="progress">The current value of the progress bar.</param>
        /// <param name="guiLayoutOptions">Optional, <see cref="GUILayoutOption"/>s.</param>
        public static void DrawProgressBar(GUIContent content, float progress, params GUILayoutOption[] guiLayoutOptions)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content);
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, guiLayoutOptions);
            EditorGUI.ProgressBar(rect, progress, $"{progress * 100f}%");
            EditorGUILayout.EndHorizontal();
        }
    }
}
