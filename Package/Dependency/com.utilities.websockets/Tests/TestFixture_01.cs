// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Utilities.WebSockets.Tests
{
    internal class TestFixture_01
    {
        [Test]
        public async Task Test_01_SimpleEcho()
        {
            var openTcs = new TaskCompletionSource<bool>();
            var messageTcs = new TaskCompletionSource<bool>();
            var closeTcs = new TaskCompletionSource<bool>();
            using var socket = new WebSocket("wss://echo.websocket.events");
            socket.OnOpen += Socket_OnOpen;
            socket.OnMessage += Socket_OnMessage;
            socket.OnError += Socket_OnError;
            socket.OnClose += Socket_OnClose;
            socket.Connect();

            var result = await openTcs.Task;
            Assert.IsTrue(result);
            await socket.SendAsync("hello world!");
            result = await messageTcs.Task;
            Assert.IsTrue(result);
            await socket.CloseAsync();
            result = await closeTcs.Task;
            Assert.IsTrue(result);

            void Socket_OnOpen()
            {
                UnityEngine.Debug.Log("Socket_OnOpen");
                openTcs.TrySetResult(true);
            }

            void Socket_OnMessage(DataFrame dataFrame)
            {
                UnityEngine.Debug.Log($"Socket_OnMessage: {dataFrame.Text}");
                messageTcs.TrySetResult(true);
            }

            void Socket_OnError(Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                Assert.Fail(exception.Message);
            }

            void Socket_OnClose(CloseStatusCode arg1, string arg2)
            {
                UnityEngine.Debug.Log("Socket_OnClose");
                closeTcs.TrySetResult(true);
            }
        }
    }
}
