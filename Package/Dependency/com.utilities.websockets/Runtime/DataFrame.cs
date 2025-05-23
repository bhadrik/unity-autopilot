// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Utilities.WebSockets
{
    public class DataFrame
    {
        public OpCode Type { get; }

        public ReadOnlyMemory<byte> Data { get; }

        public string Text { get; }

        public DataFrame(OpCode type, ReadOnlyMemory<byte> data)
        {
            Type = type;
            Data = data;
            Text = type == OpCode.Text
                ? System.Text.Encoding.UTF8.GetString(data.Span)
                : string.Empty;
        }
    }
}
