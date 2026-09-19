using GhostfolioSidekick.AI.Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
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

		// Number of messages in the stored (cleaned) history that were already processed by FixupMemory.
		private int _fixedUpCount;

		// Tool-call messages waiting to be merged into the next non-tool message's "tool_call" property.
		private readonly List<ChatMessage> _pendingToolCalls = [];

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

			mainAgent = GhostfolioSidekick.Create(chatClient, allTools);

			this.logger = logger;
		}

		public IReadOnlyCollection<ChatMessage> History()
		{
			if (session == null)
			{
				return [];
			}

			FixupMemory();

			if (session.TryGetInMemoryChatHistory(out var chatHistory))
			{
				return chatHistory.Where(x => x.Text != null).ToList();
			}

			return [];
		}

		private void FixupMemory()
		{
			if (session == null)
			{
				return;
			}

			if (!session.TryGetInMemoryChatHistory(out var chatHistory))
			{
				return;
			}

			// Stored history shrank (replaced externally): reprocess from scratch.
			if (chatHistory.Count < _fixedUpCount)
			{
				_fixedUpCount = 0;
				_pendingToolCalls.Clear();
			}

			if (chatHistory.Count == _fixedUpCount)
			{
				return;
			}

			var cleanedChatHistory = chatHistory.Take(_fixedUpCount).ToList();

			foreach (var message in chatHistory.Skip(_fixedUpCount))
			{
				if (string.IsNullOrWhiteSpace(message.Text))
				{
					continue;
				}

				// Add toolcall messages as additional properties to the next message from the agent, so that they can be displayed in the UI.
				if (message.Role == ChatRole.Tool)
				{
					_pendingToolCalls.Add(message);
					continue;
				}

				if (message.Role == ChatRole.User)
				{
					message.AuthorName = "User";
				}

				message.AdditionalProperties ??= [];

				if (!message.AdditionalProperties.Any(x => x.Key == "tool_call"))
				{
					message.AdditionalProperties.TryAdd("tool_call", "");
				}

				if (_pendingToolCalls.Count != 0)
				{
					message.AdditionalProperties["tool_call"] = string.Join(", ", _pendingToolCalls.Select(tc => tc.Text ?? string.Empty));
					_pendingToolCalls.Clear();
				}

				cleanedChatHistory.Add(message);
			}

			_fixedUpCount = cleanedChatHistory.Count;
			session.SetInMemoryChatHistory(cleanedChatHistory);
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
			_fixedUpCount = 0;
			_pendingToolCalls.Clear();
		}
	}
}
