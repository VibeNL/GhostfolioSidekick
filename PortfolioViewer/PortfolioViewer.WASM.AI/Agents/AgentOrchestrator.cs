using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
    public class AgentOrchestrator
    {
        private readonly Kernel _kernel;
		private readonly AgentGroupChat _groupChat;
		private readonly Dictionary<string, KernelAgent> _agentMap;

        public AgentOrchestrator(Kernel kernel, IEnumerable<KernelAgent> agents)
        {
            _kernel = kernel;
            _groupChat = new AgentGroupChat();
            _agentMap = agents.ToDictionary(a => a.Name, a => a);
            foreach (var agent in agents)
            {
                _groupChat.AddAgent(agent);
            }
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetCombinedResponseAsync(IEnumerable<ChatMessage> input, AgentContext context)
        {
            // Convert input to SK ChatHistory
            var chatHistory = new ChatHistory();
            foreach (var msg in input)
            {
                chatHistory.AddMessage(msg.Role.ToString(), msg.Text);
            }

            // Run the group chat
            var result = _groupChat.InvokeAsync(_kernel, chatHistory);

            await foreach (var update in result)
            {
                // Map SK chat update to your ChatResponseUpdate
                if (!string.IsNullOrWhiteSpace(update.Content))
                {
                    yield return new ChatResponseUpdate(ChatRole.Assistant, update.Content);
                }
            }
        }
    }
}
