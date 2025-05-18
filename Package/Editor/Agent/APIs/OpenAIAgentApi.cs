using Cysharp.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using System;
using System.Collections.Generic;

namespace UnityAutopilot.Agent.APIs
{
    public class OpenAIAgentApi : IAgentApi
    {
        private readonly OpenAIClient openAIClient;

        public string ModelName { get; private set; }

        public OpenAIAgentApi(string apikey, string modelName)
        {
            this.ModelName = modelName;
            var auth = new OpenAIAuthentication(apiKey: apikey);
            var settings = new OpenAISettings();
            openAIClient = new OpenAIClient(auth, settings);
        }

        public async UniTask<IReadOnlyList<Model>> GetModels()
        {
            return await openAIClient.ModelsEndpoint.GetModelsAsync().AsUniTask();
        }

        public async UniTask<AgentResponse> ChatCompletion(IReadOnlyList<Message> messages, IReadOnlyList<ToolCall> tools)
        {
            ChatRequest chatRequest = new ChatRequest(
                messages: GetGPTChatHistory(messages),
                tools: GetGPTToolList(tools),
                model: ModelName
                );

            var response = await openAIClient.ChatEndpoint.GetCompletionAsync(chatRequest);

            return ConvertResponseFromGPT(response);
        }

        private IEnumerable<OpenAI.Chat.Message> GetGPTChatHistory(IReadOnlyList<Message> messages)
        {
            List<OpenAI.Chat.Message> msgs = new List<OpenAI.Chat.Message>();

            foreach (var message in messages)
            {
                OpenAI.Role gptRole = Enum.Parse<OpenAI.Role>(message.role.ToString());

                if (message.tool_calls != null && message.tool_calls.Count > 0)
                {
                    var toolCall = new List<OpenAI.ToolCall>();

                    foreach (var tool in message.tool_calls)
                    {
                        toolCall.Add(new OpenAI.ToolCall(tool.id, tool.function.name, tool.function.arguments));
                    }

                    var m = new OpenAI.Chat.Message(toolCall, message.content);
                    msgs.Add(m);
                }
                else if (gptRole == OpenAI.Role.Tool)
                {
                    var m = new OpenAI.Chat.Message(message.tool_call_id, message.name, new Content[] { new Content(message.content) });
                    msgs.Add(m);
                }
                else
                {
                    msgs.Add(new OpenAI.Chat.Message(role: gptRole, content: message.content));
                }

            }

            return msgs;
        }

        private IEnumerable<OpenAI.Tool> GetGPTToolList(IReadOnlyList<ToolCall> tools)
        {
            List<OpenAI.Tool> gptTools = new List<OpenAI.Tool>();
            foreach (var tool in tools)
            {
                gptTools.Add(new OpenAI.Tool(tool.id, tool.index, tool.type, new OpenAI.Function(tool.function.name, tool.function.description, tool.function.parameters)));
            }
            return gptTools;
        }

        /// <summary>
        /// Convert standard chat bot response to LLMResponse
        /// </summary>
        /// <param name="response">Expecting standard chat bot reply</param>
        /// <returns></returns>
        private AgentResponse ConvertResponseFromGPT(ChatResponse response)
        {
            var msg = new Message()
            {
                role = Enum.Parse<Role>(response.FirstChoice.Message.Role.ToString()),
                content = response.FirstChoice.Message.Content as string,
                name = response.FirstChoice.Message.Name,
                refusal = response.FirstChoice.Message.Refusal,
                tool_call_id = response.FirstChoice.Message.ToolCallId
            };

            if (response.FirstChoice.Message.ToolCalls != null)
            {
                msg.tool_calls = new List<ToolCall>();
                foreach (var tool in response.FirstChoice.Message.ToolCalls)
                {
                    msg.tool_calls.Add(new ToolCall()
                    {
                        id = tool.Id,
                        index = tool.Index.HasValue ? tool.Index.Value : 0,
                        type = tool.Type,
                        function = new Function()
                        {
                            name = tool.Function.Name,
                            description = tool.Function.Description,
                            parameters = tool.Function.Parameters,
                            arguments = tool.Function.Arguments,
                            strict = tool.Function.Strict
                        }
                    });
                }
            }

            var rs = new AgentResponse()
            {
                id = response.Id,
                objectType = response.Object,
                created = response.CreatedAtUnixTimeSeconds,
                model = response.Model,
                choices = new List<AgentResponse.Choice>()
                {
                    new AgentResponse.Choice()
                    {
                        index = 0,
                        message = msg,
                        finish_reason = response.FirstChoice.FinishReason,
                    }
                },
                usage = new AgentResponse.Usage()
            };

            return rs;
        }
    }
}