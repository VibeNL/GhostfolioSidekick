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

			List<ChatMessage> cleanedChatHistory = [];
			if (session.TryGetInMemoryChatHistory(out var chatHistory))
			{
				// Add toolcall messages as additional properties to the next message from the agent, so that they can be displayed in the UI.
				List<ChatMessage> toolsCalls = [];
				foreach (var message in chatHistory.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
				{
					if (message.Role == ChatRole.Tool)
					{
						toolsCalls.Add(message);
					}
					else
					{
						if (message.Role == ChatRole.User)
						{
							message.AuthorName = "User";
						}

						message.AdditionalProperties ??= [];

						if (!message.AdditionalProperties.Any(x => x.Key == "tool_call"))
						{
							message.AdditionalProperties.TryAdd("tool_call", "");
						}

						if (toolsCalls.Count != 0)
						{
							message.AdditionalProperties["tool_call"] = string.Join(", ", toolsCalls.Select(tc => tc.Text ?? string.Empty));
							toolsCalls.Clear();
						}

						// Remove <think>content</think> tags from the message text, as they are only used for formatting in the UI and can be confusing when displayed as-is.
						//message.Contents =
						//	[.. message.Contents.Select(content =>
						//	{
						//		if (content is TextContent textContent)
						//		{
						//			textContent.Text = System.Text.RegularExpressions.Regex.Replace(textContent.Text?.ToString() ?? string.Empty, @"<think>(.*?)</think>", "", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromMinutes(1));
						//		}

						//		return content;
						//	})];

						cleanedChatHistory.Add(message);
					}
				}
			}

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
		}
	}
}
