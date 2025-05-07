using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public class AgentOrchestrator
	{
		private readonly List<IAgent> _agents;

		public AgentOrchestrator(IWebChatClient chatClient, List<IAgent> agents)
		{
			_agents = agents;
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetCombinedResponseAsync(IEnumerable<ChatMessage> input, AgentContext context)
		{
			foreach (var agent in _agents) // TODO, make this smart
			{
				await foreach (var response in agent.RespondAsync(input, context))
				{
					if (!string.IsNullOrWhiteSpace(response.Text))
					{
						yield return response;
					}
				}
			}
		}
	}
}
