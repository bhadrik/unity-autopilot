// Licensed under the MIT License. See LICENSE in the project root for license information.

#if PLATFORM_WEBGL && !UNITY_EDITOR

using AOT;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utilities.Async;
using Newtonsoft.Json;

namespace Utilities.WebSockets
{
    public class WebSocket : IWebSocket
    {
        public WebSocket(string url, IReadOnlyDictionary<string, string> requestHeaders = null, IReadOnlyList<string> subProtocols = null)
            : this(new Uri(url), requestHeaders, subProtocols)
        {
        }

        public WebSocket(Uri uri, IReadOnlyDictionary<string, string> requestHeaders = null, IReadOnlyList<string> subProtocols = null)
        {
            var protocol = uri.Scheme;

            if (!protocol.Equals("ws") && !protocol.Equals("wss"))
            {
                throw new ArgumentException($"Unsupported protocol: {protocol}");
            }

            if (requestHeaders is { Count: > 0 })
            {
                Debug.LogWarning("Request Headers are not supported in WebGL and will be ignored.");
            }

            Address = uri;
            SubProtocols = subProtocols ?? new List<string>();
            RequestHeaders = requestHeaders ?? new Dictionary<string, string>();
            _socket = WebSocket_Create(
                uri.ToString(),
                JsonConvert.SerializeObject(SubProtocols),
                WebSocket_OnOpen,
                WebSocket_OnMessage,
                WebSocket_OnError,
                WebSocket_OnClose);

            if (_socket == IntPtr.Zero || !_sockets.TryAdd(_socket, this))
            {
                throw new InvalidOperationException("Failed to create WebSocket instance!");
            }

            RunMessageQueue();
        }

        ~WebSocket()
        {
            Dispose(false);
        }

        private async void RunMessageQueue()
        {
            while (_semaphore != null)
            {
                // ensure that messages are invoked on main thread.
                await Awaiters.UnityMainThread;

                while (_events.TryDequeue(out var action))
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        OnError?.Invoke(e);
                    }
                }
            }
        }

        #region IDisposable

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    if (State == State.Open)
                    {
                        CloseAsync().Wait();
                    }

                    WebSocket_Dispose(_socket);
                    _socket = IntPtr.Zero;

                    _lifetimeCts?.Cancel();
                    _lifetimeCts?.Dispose();
                    _lifetimeCts = null;

                    _semaphore?.Dispose();
                    _semaphore = null;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region Native Interop

        private static ConcurrentDictionary<IntPtr, WebSocket> _sockets = new();

        [DllImport("__Internal")]
        private static extern IntPtr WebSocket_Create(string url, string subProtocols, WebSocket_OnOpenDelegate onOpen, WebSocket_OnMessageDelegate onMessage, WebSocket_OnErrorDelegate onError, WebSocket_OnCloseDelegate onClose);

        private delegate void WebSocket_OnOpenDelegate(IntPtr websocketPtr);

        [MonoPInvokeCallback(typeof(WebSocket_OnOpenDelegate))]
        private static void WebSocket_OnOpen(IntPtr websocketPtr)
        {
            if (_sockets.TryGetValue(websocketPtr, out var socket))
            {
                socket._events.Enqueue(() => socket.OnOpen?.Invoke());
            }
            else
            {
                Debug.LogError($"{nameof(WebSocket_OnOpen)}: Invalid websocket pointer! {websocketPtr.ToInt64()}");
            }
        }

        private delegate void WebSocket_OnMessageDelegate(IntPtr websocketPtr, IntPtr dataPtr, int length, OpCode type);

        [MonoPInvokeCallback(typeof(WebSocket_OnMessageDelegate))]
        private static void WebSocket_OnMessage(IntPtr websocketPtr, IntPtr dataPtr, int length, OpCode type)
        {
            if (_sockets.TryGetValue(websocketPtr, out var socket))
            {
                var buffer = new byte[length];
                Marshal.Copy(dataPtr, buffer, 0, length);
                socket._events.Enqueue(() => socket.OnMessage?.Invoke(new DataFrame(type, buffer)));
            }
            else
            {
                Debug.LogError($"{nameof(WebSocket_OnMessage)}: Invalid websocket pointer! {websocketPtr.ToInt64()}");
            }
        }

        private delegate void WebSocket_OnErrorDelegate(IntPtr websocketPtr, IntPtr messagePtr);

        [MonoPInvokeCallback(typeof(WebSocket_OnErrorDelegate))]
        private static void WebSocket_OnError(IntPtr websocketPtr, IntPtr messagePtr)
        {
            if (_sockets.TryGetValue(websocketPtr, out var socket))
            {
                var message = Marshal.PtrToStringUTF8(messagePtr);
                socket._events.Enqueue(() => socket.OnError?.Invoke(new Exception(message)));
            }
            else
            {
                Debug.LogError($"{nameof(WebSocket_OnError)}: Invalid websocket pointer! {websocketPtr.ToInt64()}");
            }
        }

        private delegate void WebSocket_OnCloseDelegate(IntPtr websocketPtr, CloseStatusCode code, IntPtr reasonPtr);

        [MonoPInvokeCallback(typeof(WebSocket_OnCloseDelegate))]
        private static void WebSocket_OnClose(IntPtr websocketPtr, CloseStatusCode code, IntPtr reasonPtr)
        {
            if (_sockets.TryGetValue(websocketPtr, out var socket))
            {
                var reason = Marshal.PtrToStringUTF8(reasonPtr);
                socket._events.Enqueue(() => socket.OnClose?.Invoke(code, reason));
            }
            else
            {
                Debug.LogError($"{nameof(WebSocket_OnClose)}: Invalid websocket pointer! {websocketPtr.ToInt64()}");
            }
        }

        [DllImport("__Internal")]
        private static extern int WebSocket_GetState(IntPtr websocketPtr);

        [DllImport("__Internal")]
        private static extern void WebSocket_Connect(IntPtr websocketPtr);

        [DllImport("__Internal")]
        private static extern void WebSocket_SendData(IntPtr websocketPtr, byte[] data, int length);

        [DllImport("__Internal")]
        private static extern void WebSocket_SendString(IntPtr websocketPtr, string text);

        [DllImport("__Internal")]
        private static extern void WebSocket_Close(IntPtr websocketPtr, CloseStatusCode code, string reason);

        [DllImport("__Internal")]
        private static extern void WebSocket_Dispose(IntPtr websocketPtr);

        #endregion Native Interop

        /// <inheritdoc />
        public event Action OnOpen;

        /// <inheritdoc />
        public event Action<DataFrame> OnMessage;

        /// <inheritdoc />
        public event Action<Exception> OnError;

        /// <inheritdoc />
        public event Action<CloseStatusCode, string> OnClose;

        /// <inheritdoc />
        public Uri Address { get; }

        public IReadOnlyDictionary<string, string> RequestHeaders { get; }

        /// <inheritdoc />
        public IReadOnlyList<string> SubProtocols { get; }

        /// <inheritdoc />
        public State State => _socket != IntPtr.Zero
            ? (State)WebSocket_GetState(_socket)
            : State.Closed;

        private readonly object _lock = new();
        private IntPtr _socket;
        private SemaphoreSlim _semaphore = new(1, 1);
        private CancellationTokenSource _lifetimeCts;
        private readonly ConcurrentQueue<Action> _events = new();

        /// <inheritdoc />
        public async void Connect()
            => await ConnectAsync();

        /// <inheritdoc />
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            WebSocket_Connect(_socket);
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task SendAsync(string text, CancellationToken cancellationToken = default)
        {
            WebSocket_SendString(_socket, text);
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task SendAsync(ArraySegment<byte> data, CancellationToken cancellationToken = default)
        {
            WebSocket_SendData(_socket, data.Array, data.Count);
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async void Close()
            => await CloseAsync();

        /// <inheritdoc />
        public async Task CloseAsync(CloseStatusCode code = CloseStatusCode.Normal, string reason = "", CancellationToken cancellationToken = default)
        {
            WebSocket_Close(_socket, code, reason);
            await Task.CompletedTask;
        }
    }
}
#endif //  PLATFORM_WEBGL && !UNITY_EDITOR
