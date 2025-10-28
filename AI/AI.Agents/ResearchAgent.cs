using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.AI.Functions;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace GhostfolioSidekick.AI.Agents
{
	public static class ResearchAgent
	{
		private static string GetSystemPrompt()
		{
			var sb = new StringBuilder();
			sb.AppendLine("You are ResearchAgent AI — a smart financial assistant.");
			sb.AppendLine($"Today is {DateTime.Now:yyyy-MM-dd}.");
			sb.AppendLine("You may call functions if needed.");
			sb.AppendLine("You can make multiple related function calls to gather comprehensive information.");
			sb.AppendLine("When you get function results, you should analyze them and provide a helpful summary.");
			sb.AppendLine("For complex research tasks, you can use the multi_step_research function to perform a series of research steps.");
			return sb.ToString();
		}

		public static ChatCompletionAgent Create(ICustomChatClient webChatClient, IServiceProvider serviceProvider)
		{
			string researchAgent = GetSystemPrompt();
			IKernelBuilder functionCallingBuilder = Kernel.CreateBuilder();
			functionCallingBuilder.Services.AddScoped((s) =>
			{
				var client = webChatClient.Clone();
				client.ChatMode = ChatMode.FunctionCalling;
				return client.AsChatCompletionService();
			});
			var searchService = serviceProvider.GetRequiredService<GoogleSearchService>();
			var agentlogger = serviceProvider.GetRequiredService<AgentLogger>();
			var functionCallingkernel = functionCallingBuilder.Build();
			functionCallingkernel.Plugins.AddFromObject(new ResearchAgentFunction(searchService, webChatClient.AsChatCompletionService(), agentlogger));

			var agent = new ChatCompletionAgent
			{
				Name = "ResearchAgent",
				Instructions = researchAgent,
				InstructionsRole = AuthorRole.System,
				Kernel = functionCallingkernel,
				Description = "A researcher that can access real-time data on the internet. Also can query recent financial news and perform multi-step research.",
				Arguments = new KernelArguments(new PromptExecutionSettings()
				{
					FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
				})
			};

			return agent;
		}
	}
}
