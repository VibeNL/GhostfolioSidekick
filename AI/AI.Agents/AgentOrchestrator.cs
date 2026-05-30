using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.AI.Functions;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
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
			chatClient = (ICustomChatClient)serviceProvider.GetService(typeof(ICustomChatClient))!;

			var researchAgent = ResearchAgent.Create(chatClient, serviceProvider);

			var searchService = (GoogleSearchService)serviceProvider.GetService(typeof(GoogleSearchService))!;
			var agentLogger = (AgentLogger)serviceProvider.GetService(typeof(AgentLogger))!;
			var modelInfo = (ModelInfo)serviceProvider.GetService(typeof(ModelInfo))!;
			var researchFunction = new ResearchAgentFunction(searchService, chatClient, modelInfo, agentLogger);
			var researchTool = AIFunctionFactory.Create(researchFunction.MultiStepResearch, "multi_step_research");

			var companions = new[] { (ResearchAgent.AgentName, ResearchAgent.AgentDescription) };
			mainAgent = GhostfolioSidekick.Create(chatClient, companions, [researchTool]);

			this.logger = logger;
		}

		public async Task<IReadOnlyCollection<ChatMessage>> History()
		{
			if (session == null)
			{
				return [];
			}

			var chatHistory = session.GetService<IList<ChatMessage>>();
			if (chatHistory == null)
			{
				return [];
			}

			return chatHistory.Where(x => x.Text != null).ToList();
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
	}
}
