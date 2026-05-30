using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.AI.Functions;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;

namespace GhostfolioSidekick.AI.Agents
{
	public static class ResearchAgent
	{
		public const string AgentName = "ResearchAgent";
		public const string AgentDescription = "A researcher that can access real-time data on the internet. Also can query recent financial news and perform multi-step research.";

		private static string GetSystemPrompt()
		{
			var sb = new StringBuilder();
			sb.AppendLine("You are ResearchAgent AI — a smart financial assistant.");
			sb.AppendLine($"Today is {DateTime.UtcNow:yyyy-MM-dd}.");
			sb.AppendLine("You may call functions if needed.");
			sb.AppendLine("You can make multiple related function calls to gather comprehensive information.");
			sb.AppendLine("When you get function results, you should analyze them and provide a helpful summary.");
			sb.AppendLine("For complex research tasks, you can use the multi_step_research function to perform a series of research steps.");
			return sb.ToString();
		}

		public static ChatClientAgent Create(ICustomChatClient webChatClient, IServiceProvider serviceProvider)
		{
			var cloned = webChatClient.Clone();
			cloned.ChatMode = ChatMode.FunctionCalling;

			var searchService = (GoogleSearchService)serviceProvider.GetService(typeof(GoogleSearchService))!;
			var agentLogger = (AgentLogger)serviceProvider.GetService(typeof(AgentLogger))!;
			var modelInfo = (ModelInfo)serviceProvider.GetService(typeof(ModelInfo))!;

			var researchFunction = new ResearchAgentFunction(searchService, cloned, modelInfo, agentLogger);
			var tool = AIFunctionFactory.Create(researchFunction.MultiStepResearch, "multi_step_research");

			return cloned.AsAIAgent(
				instructions: GetSystemPrompt(),
				name: AgentName,
				description: AgentDescription,
				tools: [tool]);
		}
	}
}
