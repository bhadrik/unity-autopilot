using Newtonsoft.Json;
using System;
using UnityAutopilot.Agent;
using UnityAutopilot.Utils;

namespace UnityAutopilot.Tools
{
    [Serializable]
    public class Response
    {
        public bool isSuccess;
        public string message;
        public object data;

        /// <summary>
        /// if true, this response will be sent to the agent on session reload.
        /// else immideatly.
        /// </summary>
        [JsonIgnore]
        public bool isRecompileRequired;
        [JsonIgnore]
        public string toolCallId;

        public Response(bool isSuccess, string message, object data = null)
        {
            this.isSuccess = isSuccess;
            this.message = message;
            this.data = data;
        }

        public static Response Error(string msg, object data = null)
        {
            return new Response(false, msg, data);
        }

        public static Response Success(string msg, object data = null)
        {
            return new Response(true, msg, data);
        }

        public override string ToString()
        {
            return Json.convert.SerializeObject(this);
        }
    }

    [Serializable]
    public class PendingResponse
    {
        public ToolCall toolCall;
        public Response response;
    }
}
