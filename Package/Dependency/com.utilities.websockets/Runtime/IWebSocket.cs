// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities.WebSockets
{
    public interface IWebSocket : IDisposable
    {
        /// <summary>
        /// Occurs when the <see cref="IWebSocket"/> connection has been established.
        /// </summary>
        event Action OnOpen;

        /// <summary>
        /// Occurs when the <see cref="IWebSocket"/> receives a message.
        /// </summary>
        event Action<DataFrame> OnMessage;

        /// <summary>
        /// Occurs when the <see cref="IWebSocket"/> raises an error.
        /// </summary>
        event Action<Exception> OnError;

        /// <summary>
        /// Occurs when the <see cref="IWebSocket"/> connection has been closed.
        /// </summary>
        event Action<CloseStatusCode, string> OnClose;

        /// <summary>
        /// The address of the <see cref="IWebSocket"/>.
        /// </summary>
        Uri Address { get; }

        /// <summary>
        /// The request headers used by the <see cref="IWebSocket"/>.
        /// </summary>
        IReadOnlyDictionary<string, string> RequestHeaders { get; }

        /// <summary>
        /// The sub-protocols used by the <see cref="IWebSocket"/>.
        /// </summary>
        IReadOnlyList<string> SubProtocols { get; }

        /// <summary>
        /// The current state of the <see cref="IWebSocket"/>.
        /// </summary>
        State State { get; }

        /// <summary>
        /// Connect to the <see cref="IWebSocket"/> server.
        /// </summary>
        void Connect();

        /// <summary>
        /// Connect to the <see cref="IWebSocket"/> server asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a text message to the <see cref="IWebSocket"/>.
        /// </summary>
        /// <param name="text">The text message to send.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        Task SendAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a binary message to the <see cref="IWebSocket"/>.
        /// </summary>
        /// <param name="data">The binary message to send.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        Task SendAsync(ArraySegment<byte> data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Close the <see cref="IWebSocket"/>.
        /// </summary>
        void Close();

        /// <summary>
        /// Close the <see cref="IWebSocket"/> asynchronously.
        /// </summary>
        /// <param name="code">The close status code.</param>
        /// <param name="reason">The reason for closing the connection.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        Task CloseAsync(CloseStatusCode code = CloseStatusCode.Normal, string reason = "", CancellationToken cancellationToken = default);
    }
}
