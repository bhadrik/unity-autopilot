// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Utilities.WebSockets
{
    /// <summary>
    /// Indicates the state of the <see cref="IWebSocket"/>
    /// </summary>
    /// <remarks>
    /// The values of this enumeration are defined in <see href="http://www.w3.org/TR/websockets/#dom-websocket-readystate"/>
    /// </remarks>
    public enum State : ushort
    {
        /// <summary>
        /// The connection has not yet been established.
        /// </summary>
        Connecting = 0,
        /// <summary>
        /// The connection has been established and communication is possible.
        /// </summary>
        Open = 1,
        /// <summary>
        /// The connection is going through the closing handshake or close has been requested.
        /// </summary>
        Closing = 2,
        /// <summary>
        /// The connection has been closed or could not be opened.
        /// </summary>
        Closed = 3
    }
}
