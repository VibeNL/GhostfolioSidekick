using GhostfolioSidekick.AI.Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.AI.Agents
{
	[ExcludeFromCodeCoverage]
	public class AgentOrchestrator
	{
		private readonly ChatClientAgent mainAgent;
		private readonly AgentLogger logger;
		private readonly ICustomChatClient chatClient;
		private AgentSession? session;

		public AgentOrchestrator(IServiceProvider serviceProvider, AgentLogger logger, ILogger<AgentOrchestrator> log)
		{
			chatClient = serviceProvider.GetRequiredService<ICustomChatClient>();

			var toolProviders = serviceProvider.GetServices<IAgentToolProvider>().ToList();
			var allTools = new List<AITool>();

			foreach (var provider in toolProviders)
			{
				foreach (var tool in provider.GetTools())
				{
					if (!allTools.Any(t => string.Equals(t.Name, tool.Name, StringComparison.OrdinalIgnoreCase)))
					{
						allTools.Add(tool);
					}
					else
					{
						log.LogWarning("Duplicate AI tool '{ToolName}' from provider '{ProviderName}'; keeping the first registration.", tool.Name, provider.ProviderName);
					}
				}
			}

			var capabilities = string.Join(Environment.NewLine, toolProviders.Select(p => $"- {p.ProviderName}: {p.ProviderDescription}"));
			mainAgent = GhostfolioSidekick.Create(chatClient, allTools, capabilities);

			this.logger = logger;
		}

		public IReadOnlyCollection<ChatMessage> History()
		{
			if (session == null)
			{
				return [];
			}

			if (!session.TryGetInMemoryChatHistory(out var chatHistory))
			{
				return [];
			}

			// Project the history for display: drop tool turns (their results are already folded into the final answer) and normalize user author names.
			return chatHistory
				.Where(x => x.Text != null && x.Role != ChatRole.Tool)
				.Select(x => x.Role == ChatRole.User ? new ChatMessage(ChatRole.User, x.Text) { AuthorName = "User" } : x)
				.ToList();
		}

		public async IAsyncEnumerable<AgentResponseUpdate> AskQuestion(string input)
		{
			logger.StartAgent(mainAgent.Name ?? "<????>");

			if (session == null)
			{
				session = await mainAgent.CreateSessionAsync();
			}

			await foreach (var update in mainAgent.RunStreamingAsync(input, session))
			{
				logger.StartAgent(update.AuthorName ?? mainAgent.Name ?? string.Empty);
				yield return update;
			}

			logger.StartAgent(string.Empty);
		}

		public Task InitializeAsync(Progress<InitializeProgress> progress)
		{
			return chatClient.InitializeAsync(progress);
		}

		public void ClearMemory()
		{
			session = null;
		}
	}
}
