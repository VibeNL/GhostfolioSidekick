using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace PortfolioViewer.WASM.AI.Agents
{
	public class AgentCoordinator : IAgentCoordinator
	{
		private readonly IChatClient _chatClient;
		private readonly IEnumerable<IAgent> _agents;

		public AgentCoordinator(
			IChatClient chatClient,
			IEnumerable<IAgent> agents)
		{
			_chatClient = chatClient;
			_agents = agents;
		}

		public async IAsyncEnumerable<ChatResponseUpdate> RunAgentsAsync(IEnumerable<ChatMessage> messages)
		{
			var tasks = await ParseInputWithLlmAsync(messages);

			var results = new List<string>();
			foreach (var task in tasks)
			{
				foreach (var agent in _agents)
				{
					var result = await agent.HandleAsync(task, new AgentContext());
					if (!string.IsNullOrWhiteSpace(result))
					{
						results.Add($"**{task.Trim()}**\n{result.Trim()}\n");
					}
				}
			}

			// TODO, make it more intelligent

			yield return new ChatResponseUpdate(ChatRole.Assistant, string.Join("\n", results));
		}

		private async Task<List<string>> ParseInputWithLlmAsync(IEnumerable<ChatMessage> input)
		{
			var prompt = @$"""
            You have access to agents that can help with specific tasks. 
			Your job is to break down the input into smaller, manageable tasks that can be handled by these agents.

			Here are the tasks you can assign to the agents:
			- PortfolioLogicAgent, can query the current portfolio and provide insights.
			           
            Return each task as a bullet point (no explanations), prefixed with [TASK].
			You may choose to call multiple agents for a single task, but please keep the tasks simple and clear.
			You also may choose not to call any agent if there is no need for it.
			""";

			var response = await _chatClient.GetResponseAsync(AppendToSystem(input, prompt));

			// Split by newlines and strip bullets
			var tasks = response.Text
				.Split('\n')
				.Where(line => !string.IsNullOrWhiteSpace(line))
				.Where(line => line.StartsWith("[TASK]", StringComparison.InvariantCultureIgnoreCase))
				.Select(line => line.TrimStart('-', '*', '•', ' ').Trim())
				.ToList();

			return tasks.Count > 0 ? tasks : [.. input.Select(x => x.Text)];
		}

		private IEnumerable<ChatMessage> AppendToSystem(IEnumerable<ChatMessage> input, string prompt)
		{
			var list = input.ToList();
			var lastSystemMessageIndex = list.FindLastIndex(x => x.Role == ChatRole.System);
			if (lastSystemMessageIndex > -1)
			{
				var lastSystemMessage = list[lastSystemMessageIndex];
				list[lastSystemMessageIndex] = new ChatMessage(ChatRole.System, lastSystemMessage.Text + Environment.NewLine + prompt);
			}

			return list;
		}

	}
}
