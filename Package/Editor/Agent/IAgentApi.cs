using Cysharp.Threading.Tasks;
using OpenAI.Models;
using System.Collections.Generic;

namespace UnityAutopilot.Agent
{
    public interface IAgentApi
    {
        // for connection test (API validation)
        UniTask<IReadOnlyList<Model>> GetModels();
        UniTask<AgentResponse> ChatCompletion(IReadOnlyList<Message> messages, IReadOnlyList<ToolCall> tools);

        string ModelName { get; }
    }
}