using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using UnityAutopilot.Agent;
using UnityAutopilot.Agent.APIs;
using UnityAutopilot.Tools;
using UnityEditor;
using UnityEngine;

namespace UnityAutopilot
{
    public class UnityAutopilotWindow : EditorWindow
    {
        /// <summary>
        /// Default path for generated assets
        /// </summary>
        public const string DEFAULT_GENERATED_ASSET_PATH = "Assets/Autopilot Generated";

        private const string WindowTitle = "Autopilot";

        private string input = string.Empty;
        private Vector2 scroll;
        private string log = string.Empty;
        private Vector2 toolBtnPos;
        private bool showTooltips;

        private const string OPENAI_MODEL = "gpt-4.1-2025-04-14";

        string ApiKey
        {
            get => EditorPrefs.GetString("AIEditorAgent_OpenAIKey", "");
            set => EditorPrefs.SetString("AIEditorAgent_OpenAIKey", value);
        }

        string apiKeyInput;

        private static IAgentApi openAIApi = null;


        [MenuItem("Window/Autopilot/Chat %#&a")]
        public static void Open()
        {
            var w = GetWindow<UnityAutopilotWindow>();
            w.titleContent = new GUIContent(WindowTitle);
            w.minSize = new Vector2(420, 320);
        }


        private void OnEnable()
        {
            log = string.Empty;

            ToolManager.RefreshTools();

            if (string.IsNullOrEmpty(ApiKey))
            {
                SessionManager.Setup(null);

                return;
            }

            try
            {
                openAIApi = new OpenAIAgentApi(ApiKey, OPENAI_MODEL);

                SessionManager.Setup(openAIApi);
            }
            catch (Exception e)
            {
                SessionManager.Setup(null);
                Utils.Logger.LogSimple($"<color={Utils.Logger.faild}><b>Autopilot connection faild.</b></color> Error => {e.Message}");
            }

            SessionManager.OnMessageUpdate += AddMessageLog;
            SessionManager.OnChatResetCall += OnChatRestCall;
        }

        private void OnDisable()
        {
            SessionManager.OnMessageUpdate -= AddMessageLog;
            SessionManager.OnChatResetCall -= OnChatRestCall;

            SessionManager.ClearSession();
        }

        #region Inspector GUI

        /// <summary>
        /// Render the window
        /// Need to update to use UI Toolkit
        /// </summary>
        private void OnGUI()
        {
            DrawSessionHeader();

            if (!SessionManager.IsNetworkReachable)
            {
                DrawNetworkIssue();
                return;
            }

            if (string.IsNullOrEmpty(ApiKey) || SessionManager.IsMainAgentCrationFailed)
            {
                DrawApiKeySetup(SessionManager.IsMainAgentCrationFailed);
                return;
            }

            DrawLogView();
            DrawToolExecutionButtons();
            DrawToolToggle();
            DrawPromptInput();
            DrawSessionControls();
            DrawTooltips();

            //if (GUILayout.Button("Remove key"))
            //{
            //    ApiKey = string.Empty;

            //    SessionManager.Setup(null);
            //}
        }

        private void DrawNetworkIssue()
        {
            EditorGUILayout.HelpBox("Network issue. Please check your internet connection.", MessageType.Error);
            EditorGUILayout.Space(5);
            if(GUILayout.Button("Retry"))
            {
                OnEnable();
            }
            EditorGUILayout.Space(5);
        }

        private void DrawSessionHeader()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Session: {SessionManager.CurrentSessionId}");

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_TextAsset Icon"), GUILayout.Width(24), GUILayout.Height(24)))
                SessionManager.PingCurrentSessionFile();

            GUILayout.Button(EditorGUIUtility.IconContent("CustomTool"), GUILayout.Width(24), GUILayout.Height(24));
            toolBtnPos = GUILayoutUtility.GetLastRect().position;
            showTooltips = Event.current.type == EventType.Repaint &&
                           GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawApiKeySetup(bool apiConnectionFaild)
        {
            if (apiConnectionFaild)
            {
                EditorGUILayout.HelpBox("API connection failed. Please check your API key.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("Please set your OpenAI API key to start using the agent.", MessageType.Warning);
            }

            apiKeyInput = EditorGUILayout.PasswordField("API key", apiKeyInput);

            if (GUILayout.Button("Save API Key"))
            {
                ApiKey = apiKeyInput;
                OnEnable();
            }
            EditorGUILayout.Space();
        }

        private void DrawLogView()
        {
            GUIStyle richTextStyle = new GUIStyle()
            {
                wordWrap = true,
                richText = true,
                normal = { textColor = Color.white },
                margin = { bottom = 10 }
            };

            scroll = EditorGUILayout.BeginScrollView(scroll, EditorStyles.helpBox, GUILayout.MinHeight(160));
            EditorGUILayout.TextArea(log, richTextStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
        }

        private void DrawToolExecutionButtons()
        {
            if (!SessionManager.IsToolExecWaiting) return;

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.25f, 0.76f, 0.96f);
            if (GUILayout.Button("Run command", GUILayout.Height(40)))
                SessionManager.ExecWaitingCommand = true;

            GUI.backgroundColor = new Color(0.96f, 0.25f, 0.26f);
            if (GUILayout.Button("Cancel command", GUILayout.Height(40)))
                SessionManager.DiscardWaitingCommand = true;

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

        private void DrawToolToggle()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Auto allow tool run");
            SessionManager.allowAllTools = EditorGUILayout.Toggle(SessionManager.allowAllTools);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void DrawPromptInput()
        {
            GUI.enabled = !SessionManager.IsToolExecWaiting;

            input = EditorGUILayout.TextArea(input, new GUIStyle(EditorStyles.textArea) { wordWrap = true }, GUILayout.Height(50));

            GUI.enabled = SessionManager.IsSessionRunning && !SessionManager.IsToolExecWaiting;
            if (GUILayout.Button("Send", GUILayout.Height(28)))
            {
                GUI.FocusControl(null);
                SessionManager.SendPromptAsync(input).Forget();
                input = string.Empty;
            }

            GUI.enabled = true;
            EditorGUILayout.Space(5);
        }

        private void DrawSessionControls()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("New Session", GUILayout.Height(28)))
            {
                GUI.FocusControl(null);
                SessionManager.CreateNewSession().Forget();
            }

            if (GUILayout.Button("Browse session", GUILayout.Height(28)))
            {
                GUI.FocusControl(null);
                SessionManager.LoadSessionFromFile().Forget();
            }

            if (GUILayout.Button("Reload session", GUILayout.Height(28)))
            {
                GUI.FocusControl(null);
                string id = EditorPrefs.GetString("AIEditorAgent_RecentSessionID", "");
                if (!string.IsNullOrWhiteSpace(id))
                    SessionManager.LoadSession(id).Forget();
                else
                    Debug.LogError("Last session not found. You can load using browsing file.");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }

        private void DrawTooltips()
        {
            if (!showTooltips) return;

            GUIContent tooltipContent = new GUIContent(string.Join("\n", GetRegisteredTools()));
            GUIStyle tooltipStyle = new GUIStyle(GUI.skin.label)
            {
                border = new RectOffset(4, 4, 4, 4),
                margin = new RectOffset(5, 5, 5, 5),
                padding = new RectOffset(10, 10, 5, 5),
                fontSize = 12,
                normal = { background = CreateSolidColorTexture(new Color(0.2f, 0.2f, 0.2f, 1f)) }
            };

            Vector2 tooltipSize = tooltipStyle.CalcSize(tooltipContent);
            float xPos = toolBtnPos.x - tooltipSize.x - 5;
            GUI.Label(new Rect(xPos, toolBtnPos.y, tooltipSize.x, tooltipSize.y), tooltipContent, tooltipStyle);
        }

        #endregion

        private void OnChatRestCall()
        {
            log = string.Empty;
            Repaint();
        }

        private Texture2D CreateSolidColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }


        private void AddMessageLog(Message msg)
        {
            // No need to display system message in log box
            if (msg.role == Role.System) return;

            var color = GetColorForRole(msg.role);

            var printContent = string.Empty;
            var sender = string.Empty;

            if (msg.role == Role.Assistant && msg.tool_calls != null && msg.tool_calls.Count > 0)
            {
                int counter = 1;

                StringBuilder sb = new StringBuilder();

                if (!string.IsNullOrEmpty(msg.content as string))
                    sb.AppendLine($"Content: {msg.content}");

                foreach (var toolCall in msg.tool_calls)
                {
                    sb.AppendLine($"[{counter++}] {toolCall.function.name} => {toolCall.function.arguments}");
                }
                sender = $"Tool Call";

                printContent = sb.ToString();
            }
            else
            {
                printContent = msg.content as string;

                if (msg.role == Role.Tool)
                    sender = "Tool reply";
                else
                    sender = msg.role.ToString();
            }

            log += $"<color={color}>[{sender}]</color>: {printContent}\n";

            Repaint();
        }

        private string GetColorForRole(Role role)
        {
            string color = "";

            switch (role)
            {
                case Role.Assistant:
                    color = "#bbf268";
                    break;
                case Role.System:
                    color = "#8a8a8a";
                    break;
                case Role.User:
                    color = "#3fd1eb";
                    break;
                case Role.Tool:
                    color = "#f5d95d";
                    break;
            }

            return color;
        }

        private string[] GetRegisteredTools()
        {
            List<string> tools = new();

            foreach (var tool in ToolRegistry.Registry)
            {
                tools.Add(tool.Key);
            }

            return tools.ToArray();
        }
    }
}