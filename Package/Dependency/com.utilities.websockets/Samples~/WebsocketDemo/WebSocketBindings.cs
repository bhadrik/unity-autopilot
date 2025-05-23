// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace Utilities.WebSockets.Sample
{
    [RequireComponent(typeof(UIDocument))]
    public class WebSocketBindings : MonoBehaviour
    {
        [SerializeField]
        private UIDocument uiDocument;

        [SerializeField]
        private string address = "wss://echo.websocket.events";

        private Label statusLabel;
        private Label fpsLabel;
        private TextField addressTextField;
        private Button connectButton;
        private Button disconnectButton;
        private TextField sendMessageTextField;
        private VisualElement sendMessageButtonGroup;
        private Button sendTextButton;
        private Button sendBytesButton;
        private Button sendText1000Button;
        private Button sendBytes1000Button;
        private Toggle logMessagesToggle;
        private Label sendCountLabel;
        private Label receiveCountLabel;
        private Button clearLogsButton;
        private ListView messageListView;

        private int frame;
        private int sendCount;
        private int receiveCount;

        private float time;
        private float fps;

        private WebSocket webSocket;

        private readonly List<Tuple<LogType, string>> logs = new();
#if !UNITY_2022_3_OR_NEWER
        // ReSharper disable once InconsistentNaming
        private CancellationToken destroyCancellationToken => destroyCancellationTokenSource.Token;
        private CancellationTokenSource destroyCancellationTokenSource = new CancellationTokenSource();
#endif

        private void OnValidate()
        {
            if (!uiDocument)
            {
                uiDocument = GetComponent<UIDocument>();
            }
        }

        private void Awake()
        {
            OnValidate();

            var root = uiDocument.rootVisualElement;
            statusLabel = root.Q<Label>("status-label");
            fpsLabel = root.Q<Label>("fps-label");
            addressTextField = root.Q<TextField>("address-text-field");
            addressTextField.value = address;
            connectButton = root.Q<Button>("connect-button");
            disconnectButton = root.Q<Button>("disconnect-button");
            disconnectButton.SetEnabled(false);
            sendMessageButtonGroup = root.Q<VisualElement>("send-message-button-group");
            sendMessageButtonGroup.SetEnabled(false);
            sendMessageTextField = root.Q<TextField>("send-message-text-field");
            sendTextButton = root.Q<Button>("send-text-button");
            sendBytesButton = root.Q<Button>("send-bytes-button");
            sendText1000Button = root.Q<Button>("send-text-1000-button");
            sendBytes1000Button = root.Q<Button>("send-bytes-1000-button");
            logMessagesToggle = root.Q<Toggle>("log-messages-toggle");
            sendCountLabel = root.Q<Label>("send-count-label");
            receiveCountLabel = root.Q<Label>("receive-count-label");
            clearLogsButton = root.Q<Button>("clear-logs-button");
            messageListView = root.Q<ListView>("message-list-view");
            messageListView.selectionType = SelectionType.None;
            messageListView.makeItem = () => new Label
            {
                style =
                {
                    flexGrow = 1.0f
                }
            };
            messageListView.bindItem = (element, i) =>
            {
                var (type, message) = logs[i];
                var color = type switch
                {
                    LogType.Log => "black",
                    LogType.Warning => "yellow",
                    _ => "red",
                };
                ((Label)element).text = $"<color={color}>{message}</color>";
            };
        }

        private void OnEnable()
        {
            connectButton.clicked += OnConnectClick;
            disconnectButton.clicked += OnDisconnectClick;
            sendTextButton.clicked += OnSendTextClick;
            sendBytesButton.clicked += OnSendBytesClick;
            sendText1000Button.clicked += OnSendText1000Click;
            sendBytes1000Button.clicked += OnSendBytes1000Click;
            clearLogsButton.clicked += OnClearLogsClick;
        }

        private void Update()
        {
            frame += 1;
            time += Time.unscaledDeltaTime;

            if (time >= 1f)
            {
                fps = frame / time;
                frame = 0;
                time = 0f;
            }

            var state = webSocket?.State ?? State.Closed;
            var color = state switch
            {
                State.Connecting => "yellow",
                State.Open => "green",
                _ => "red"
            };

            statusLabel.text = $"Status: <color={color}>{state}</color>";
            fpsLabel.text = $"FPS: <color=green>{fps:0}</color>";
            sendCountLabel.text = $"Send: {sendCount}";
            receiveCountLabel.text = $"Receive: {receiveCount}";
        }

        private void OnDisable()
        {
            connectButton.clicked -= OnConnectClick;
            disconnectButton.clicked -= OnDisconnectClick;
            sendTextButton.clicked -= OnSendTextClick;
            sendBytesButton.clicked -= OnSendBytesClick;
            sendText1000Button.clicked -= OnSendText1000Click;
            sendBytes1000Button.clicked -= OnSendBytes1000Click;
            clearLogsButton.clicked -= OnClearLogsClick;
        }

        private void OnDestroy()
        {
#if !UNITY_2022_3_OR_NEWER
            destroyCancellationTokenSource?.Cancel();
            destroyCancellationTokenSource?.Dispose();
            destroyCancellationTokenSource = null;
#endif
        }

        private async void OnConnectClick()
        {
            if (webSocket != null)
            {
                AddLog("Already connected!", LogType.Warning);
                return;
            }

            try
            {
                webSocket = new WebSocket(addressTextField.value);
                connectButton.SetEnabled(false);
                webSocket.OnOpen += WebSocket_OnOpen;
                webSocket.OnMessage += WebSocket_OnMessage;
                webSocket.OnError += WebSocket_OnError;
                webSocket.OnClose += WebSocket_OnClose;
                await webSocket.ConnectAsync(destroyCancellationToken);
            }
            catch (Exception e)
            {
                AddLog(e.ToString(), LogType.Exception);
            }
        }

        private async void OnDisconnectClick()
        {
            if (webSocket == null)
            {
                AddLog("No connection!", LogType.Warning);
                return;
            }

            try
            {
                await webSocket.CloseAsync(cancellationToken: CancellationToken.None);
            }
            catch (Exception e)
            {
                AddLog(e.ToString(), LogType.Exception);
            }
        }

        private async void OnSendTextClick()
        {
            if (webSocket is { State: State.Open })
            {
                try
                {
                    AddLog($"-> Sending: {sendMessageTextField.value}");
                    await webSocket.SendAsync(sendMessageTextField.value, destroyCancellationToken);
                    sendCount++;
                }
                catch (Exception e)
                {
                    AddLog(e.ToString(), LogType.Exception);
                }
            }
            else
            {
                AddLog("Websocket is not ready!", LogType.Warning);
            }
        }

        private async void OnSendBytesClick()
        {
            if (webSocket is { State: State.Open })
            {
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(sendMessageTextField.value);
                    AddLog($"-> Sending: {bytes.Length} Bytes");
                    await webSocket.SendAsync(bytes, destroyCancellationToken);
                    sendCount++;
                }
                catch (Exception e)
                {
                    AddLog(e.ToString(), LogType.Exception);
                }
            }
            else
            {
                AddLog("Websocket is not ready!", LogType.Warning);
            }
        }

        private void OnSendText1000Click()
        {
            for (int i = 0; i < 100; i++)
            {
                OnSendTextClick();
            }
        }

        private void OnSendBytes1000Click()
        {
            for (int i = 0; i < 100; i++)
            {
                OnSendBytesClick();
            }
        }

        private void OnClearLogsClick()
        {
            sendCount = 0;
            receiveCount = 0;
            logs.Clear();
            messageListView.ScrollToItem(0);
            messageListView.itemsSource = null;
            messageListView.Rebuild();
            messageListView.visible = false;
        }

        private void WebSocket_OnOpen()
        {
            disconnectButton.SetEnabled(true);
            sendMessageButtonGroup.SetEnabled(true);
            AddLog($"Websocket Connected to {address}");
        }

        private void WebSocket_OnMessage(DataFrame dataFrame)
        {
            receiveCount += 1;

            switch (dataFrame.Type)
            {
                case OpCode.Text:
                    AddLog($"<- Received: {dataFrame.Text}");
                    break;
                case OpCode.Binary:
                    AddLog($"<- Received: {dataFrame.Data.Length} Bytes");
                    break;
            }
        }

        private void WebSocket_OnError(Exception exception)
            => AddLog(exception.ToString(), LogType.Exception);

        private void WebSocket_OnClose(CloseStatusCode code, string reason)
        {
            disconnectButton.SetEnabled(false);
            sendMessageButtonGroup.SetEnabled(false);
            AddLog($"Websocket Closed: CloseStatusCode: {code}, Reason: {reason}", code == CloseStatusCode.Normal ? LogType.Log : LogType.Error);
            webSocket.OnOpen -= WebSocket_OnOpen;
            webSocket.OnMessage -= WebSocket_OnMessage;
            webSocket.OnError -= WebSocket_OnError;
            webSocket.OnClose -= WebSocket_OnClose;
            webSocket.Dispose();
            webSocket = null;
            connectButton.SetEnabled(true);
        }

        private void AddLog(string message, LogType level = LogType.Log)
        {
            if (!logMessagesToggle.value) { return; }

            switch (level)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    Debug.LogError(message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
                default:
                case LogType.Log:
                    Debug.Log(message);
                    break;
            }

            logs.Add(new(level, message));
            messageListView.itemsSource ??= logs;
            messageListView.Rebuild();

            if (!messageListView.visible)
            {
                messageListView.visible = true;
            }
        }
    }
}
