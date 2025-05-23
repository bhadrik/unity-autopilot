// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.WebSockets
{
    /// <summary>
    /// When closing an established connection (e.g., when sending a Close frame, after the opening handshake has completed),
    /// an endpoint MAY indicate a reason for closure.
    /// </summary>
    /// <remarks>
    /// The values of this enumeration are defined in <see href="http://tools.ietf.org/html/rfc6455#section-7.4"/>.<para/>
    /// </remarks>
    public enum CloseStatusCode : ushort
    {
        /// <summary>
        /// Indicates a normal closure, meaning that the purpose for which the connection was established has been fulfilled.
        /// </summary>
        Normal = 1000,
        /// <summary>
        /// Indicates that an endpoint is "going away", such as a server going down or a browser having navigated away from a page.
        /// </summary>
        GoingAway = 1001,
        /// <summary>
        /// Indicates that an endpoint is terminating the connection due to a protocol error.
        /// </summary>
        ProtocolError = 1002,
        /// <summary>
        /// Indicates that an endpoint is terminating the connection because it has received a type of data it cannot accept
        /// (e.g., an endpoint that understands only text data MAY send this if it receives a binary message).
        /// </summary>
        UnsupportedData = 1003,
        /// <summary>
        /// Reserved and MUST NOT be set as a status code in a Close control frame by an endpoint.<para/>
        /// The specific meaning might be defined in the future.
        /// </summary>
        Reserved = 1004,
        /// <summary>
        /// Reserved and MUST NOT be set as a status code in a Close control frame by an endpoint.<para/>
        /// It is designated for use in applications expecting a status code to indicate that no status code was actually present.
        /// </summary>
        NoStatus = 1005,
        /// <summary>
        /// Reserved and MUST NOT be set as a status code in a Close control frame by an endpoint.<para/>
        /// It is designated for use in applications expecting a status code to indicate that the connection was closed abnormally,
        /// e.g., without sending or receiving a Close control frame.
        /// </summary>
        AbnormalClosure = 1006,
        /// <summary>
        /// Indicates that an endpoint is terminating the connection because it has received data within a message
        /// that was not consistent with the type of the message.
        /// </summary>
        InvalidPayloadData = 1007,
        /// <summary>
        /// Indicates that an endpoint is terminating the connection because it received a message that violates its policy.
        /// This is a generic status code that can be returned when there is no other more suitable status code (e.g., 1003 or 1009)
        /// or if there is a need to hide specific details about the policy.
        /// </summary>
        PolicyViolation = 1008,
        /// <summary>
        /// Indicates that an endpoint is terminating the connection because it has received a message that is too big for it to process.
        /// </summary>
        TooBigToProcess = 1009,
        /// <summary>
        /// Indicates that an endpoint (client) is terminating the connection because it has expected the server to negotiate
        /// one or more extension, but the server didn't return them in the response message of the WebSocket handshake.
        /// The list of extensions that are needed SHOULD appear in the /reason/ part of the Close frame. Note that this status code
        /// is not used by the server, because it can fail the WebSocket handshake instead.
        /// </summary>
        MandatoryExtension = 1010,
        /// <summary>
        /// Indicates that a server is terminating the connection because it encountered an unexpected condition that prevented it from fulfilling the request.
        /// </summary>
        ServerError = 1011,
        /// <summary>
        /// Reserved and MUST NOT be set as a status code in a Close control frame by an endpoint.<para/>
        /// It is designated for use in applications expecting a status code to indicate that the connection was closed due to a failure to perform a TLS handshake
        /// (e.g., the server certificate can't be verified).
        /// </summary>
        TlsHandshakeFailure = 1015
    }
}
