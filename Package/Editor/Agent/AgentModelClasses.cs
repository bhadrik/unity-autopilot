using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using Newtonsoft.Json.Linq;

namespace UnityAutopilot.Agent
{
    [Serializable]
    public enum Role
    {
        [EnumMember(Value = "system")]
        System = 1,
        [EnumMember(Value = "developer")]
        Developer,
        [EnumMember(Value = "assistant")]
        Assistant,
        [EnumMember(Value = "user")]
        User,
        [Obsolete("Use Tool")]
        [EnumMember(Value = "function")]
        Function,
        [EnumMember(Value = "tool")]
        Tool
    }

    [Serializable]
    public class Message
    {
        public Role role;
        public string content;
        public string name;
        public string refusal;
        public List<ToolCall> tool_calls;
        public string tool_call_id;

        public Message() { }

        public Message(Role role, string content, string name = null)
        {
            this.role = role;
            this.content = content;
            this.name = name;
        }

        public Message(ToolCall toolCall, string content) : this(Role.Tool, content, name: toolCall.function.name)
        {
            this.tool_call_id = toolCall.id;
        }
    }

    [Serializable]
    public class ToolCall
    {
        public string id;
        public int index;
        public string type;
        public Function function;
    }

    [Serializable]
    public class Function
    {
        public string type;
        public string name;
        public string description;
        public JToken parameters;
        public JToken arguments;
        public bool strict;
    }

    [Serializable]
    public class AgentResponse
    {
        public string id;
        public string objectType;
        public int created;
        public string model;
        public List<Choice> choices;
        public Usage usage;

        public Choice FirstChoice
        {
            get
            {
                if (choices != null && choices.Count > 0)
                {
                    return choices[0];
                }
                return null;
            }
        }

        public class Choice
        {
            public int index;
            public Message message;
            public string finish_reason;
        }
        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
    }

    [Serializable]
    public class AgentRequest
    {
        public string model;
        public List<Message> messages;
        public int max_tokens;
        public double temperature;
        public double top_p;
        public int n;
        public bool stream;
        public double logprobs;
        public bool echo;
        public string stop;
        public double presence_penalty;
        public double frequency_penalty;
        public string user;
    }
}