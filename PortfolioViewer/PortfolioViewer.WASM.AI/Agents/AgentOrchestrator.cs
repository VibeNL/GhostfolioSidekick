using System.Text.Json;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public class AgentOrchestrator
	{
		private readonly IWebChatClient chatClient;
		private readonly IList<IAgent> _agents;

		public AgentOrchestrator(IWebChatClient chatClient, IList<IAgent> agents)
		{
			this.chatClient = chatClient;
			_agents = agents;
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetCombinedResponseAsync(IEnumerable<ChatMessage> input, AgentContext context)
		{
			var prompt = $@"You are a task routing assistant. Based on the user's message, decide which of the following specialized agents should respond:

						Agents:
						{string.Join(Environment.NewLine, _agents.Select(x => $"{x.Name}: {x.Description}"))}

						User message: ""Can you give me a quick summary of my portfolio and tell me if the market is going up or down?""

						Respond with a JSON list of agent names that should be activated. e.g. [""{_agents[0].Name}""]
						Only respond with the JSON list of agent names, nothing else. Do not include any other text or explanation.
						";

			var llmResponse = await chatClient.GetResponseAsync(prompt + input.Last());

			if (llmResponse.Text == null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, "No response from LLM.");
				yield break;
			}

			ChatMessage item = new(ChatRole.Assistant, llmResponse.Text)
			{ AuthorName = nameof(AgentOrchestrator) };
			context.Memory.Add(item);

			// Some LLM return <think>...</think> text, remove that and the content between
			var selectedAgentNames = JsonSerializer.Deserialize<List<string>>(item.ToDisplayText());

			var selectedAgents = _agents
				.Where(a => selectedAgentNames?.Contains(a.Name, StringComparer.OrdinalIgnoreCase) ?? false)
				.ToList();

			if (!selectedAgents.Any())
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, "No matching agents found.");
				yield break;
			}

			foreach (var agent in selectedAgents) // TODO, make this smart
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
