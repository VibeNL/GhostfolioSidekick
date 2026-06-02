using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Functions
{
	/// <summary>
	/// Exposes research functions as AI tools via the <see cref="IAgentToolProvider"/> extension point.
	/// </summary>
	public class ResearchAgentToolProvider : IAgentToolProvider
	{
		public string ProviderName => "Research";

		public string ProviderDescription =>
			"Provides multi-step research capabilities by searching the web and synthesizing results.";

		private readonly IReadOnlyList<AITool> _tools;

		public ResearchAgentToolProvider(
			IGoogleSearchService searchService,
			IChatClient chatService,
			ModelInfo modelInfo,
			AgentLogger agentLogger)
		{
			var functions = new ResearchAgentFunction(searchService, chatService, modelInfo, agentLogger);

			_tools =
			[
				AIFunctionFactory.Create(functions.MultiStepResearch, "multi_step_research"),
			];
		}

		public IReadOnlyList<AITool> GetTools() => _tools;
	}
}
