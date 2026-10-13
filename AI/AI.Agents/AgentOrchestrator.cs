using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.Database;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.AI.Agents
{
	[ExcludeFromCodeCoverage]
	public class AgentOrchestrator
	{
		private const string ConversationId = "main";

		private readonly ChatClientAgent mainAgent;
		private readonly AgentLogger logger;
		private readonly ICustomChatClient chatClient;
		private readonly SqliteChatHistoryProvider? historyProvider;
		private AgentSession? session;

		public AgentOrchestrator(IServiceProvider serviceProvider, AgentLogger logger)
		{
			chatClient = serviceProvider.GetRequiredService<ICustomChatClient>();

			var toolProviders = (serviceProvider.GetService(typeof(IEnumerable<IAgentToolProvider>)) as IEnumerable<IAgentToolProvider>)
				?.ToList() ?? [];
			var allTools = new List<AITool>();

			foreach (var provider in toolProviders)
			{
				allTools.AddRange(provider.GetTools());
			}

			IDbContextFactory<DatabaseContext>? dbContextFactory = (IDbContextFactory<DatabaseContext>?)serviceProvider.GetService(typeof(IDbContextFactory<DatabaseContext>));
			historyProvider = dbContextFactory is null ? null : new SqliteChatHistoryProvider(dbContextFactory, ConversationId);

			mainAgent = GhostfolioSidekick.Create(chatClient, allTools, historyProvider);
			this.logger = logger;
		}

		public async Task<IReadOnlyCollection<ChatMessage>> HistoryAsync()
		{
			if (historyProvider is null)
			{
				return [];
			}

			var messages = await historyProvider.LoadMessagesAsync();
			return messages
				.Where(x => x.Text != null)
				.Select(x =>
				{
					if (x.Role == ChatRole.User)
					{
						x.AuthorName = "User";
					}

					return x;
				})
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

		public async Task ClearMemoryAsync()
		{
			session = null;

			if (historyProvider != null)
			{
				await historyProvider.ClearAsync();
			}
		}
	}
}
