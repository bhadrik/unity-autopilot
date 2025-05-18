using Cysharp.Threading.Tasks;
using OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityAutopilot.Tools;
using UnityEngine;

namespace UnityAutopilot.Agent
{
    public class Agent
    {
        public List<Message> ChatHistory { get; private set; }
        public List<Message> CacheHistory { get; private set; }
        public List<ToolCall> ToolList { get; private set; }

        /// <summary>
        /// Agent API for chat completion and model retrieval.
        /// </summary>
        private readonly IAgentApi agentApi;

        /// <summary>
        /// History of messages exchanged with the agent.
        /// </summary>
        private readonly int historyMessageLimit = 20;

        /// <summary>
        /// Event on messaged received or sent.
        /// </summary>
        public event Action<Message> OnChatUpdate;


        public Agent(IAgentApi agentApi, History history, int historyMessageLimit)
        {
            this.agentApi = agentApi;

            this.historyMessageLimit = historyMessageLimit;

            if (history == null)
            {
                ChatHistory = new List<Message>();
                CacheHistory = new List<Message>();
                ToolList = ToolRegistry.GetRegisteredTools();
            }
            else
            {
                ChatHistory = new List<Message>(history.messages);
                CacheHistory = new List<Message>(history.messages);
                ToolList = new List<ToolCall>(history.tools);

                TrimChatHistoryToLimit();
            }
        }

        public async UniTask<AgentResponse> GetResponse()
        {
            try
            {
                var res = await agentApi.ChatCompletion(ChatHistory, ToolList);
                AddMessage(res.FirstChoice.message);

                return res;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
        }

        public async UniTask<IReadOnlyList<Model>> GetModels()
        {
            return await agentApi.GetModels();
        }

        public void AddMessage(Message message)
        {
            // message with tool call need to be added seperately
            // necessary for LLM API
            if (message.tool_calls != null && message.content != null)
            {
                var contentMsg = new Message(message.role, message.content);
                ChatHistory.Add(contentMsg);
                CacheHistory.Add(contentMsg);

                OnChatUpdate?.Invoke(contentMsg);

                // removing duplicate
                message.content = "";
            }

            ChatHistory.Add(message);
            CacheHistory.Add(message);

            TrimChatHistoryToLimit();

            OnChatUpdate?.Invoke(message);
        }

        public void LoadMessages(List<Message> messages, List<ToolCall> toolCalls)
        {
            ChatHistory = new List<Message>(messages);
            CacheHistory = new List<Message>(messages);

            ToolList = new List<ToolCall>(toolCalls);

            if (ChatHistory.Count > historyMessageLimit)
            {
                ChatHistory = ChatHistory.Skip(ChatHistory.Count - historyMessageLimit).ToList();

                TrimChatHistoryToLimit();
            }
        }

        private void TrimChatHistoryToLimit()
        {
            if (ChatHistory.Count > historyMessageLimit)
            {
                ChatHistory = ChatHistory.Skip(ChatHistory.Count - historyMessageLimit).ToList();

                // First message can not have Role Tool in the messages
                while (ChatHistory.Count > 0 && ChatHistory[0].role == Role.Tool)
                {
                    ChatHistory.RemoveAt(0);
                }
            }
        }
    }
}