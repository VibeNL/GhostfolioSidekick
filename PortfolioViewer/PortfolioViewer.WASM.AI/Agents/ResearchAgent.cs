using GhostfolioSidekick.ExternalDataProvider.DuckDuckGo;
using GhostfolioSidekick.ExternalDataProvider.Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public static class ResearchAgent
	{
		private static string GetSystemPrompt()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine("You are ResearchAgent AI — a smart financial assistant.");
			sb.AppendLine($"Today is {DateTime.Now:yyyy-MM-dd}.");
			sb.AppendLine("You may call functions if needed");
			return sb.ToString();
		}

		public static ChatCompletionAgent Create(IWebChatClient webChatClient, IServiceProvider serviceProvider)
		{
			string researchAgent = GetSystemPrompt();
			IKernelBuilder functionCallingBuilder = Kernel.CreateBuilder();
			functionCallingBuilder.Services.AddScoped<IChatCompletionService>((s) =>
			{
				var client = webChatClient.Clone();
				client.ChatMode = ChatMode.FunctionCalling;
				return client.AsChatCompletionService();
			});
			var searchService = serviceProvider.GetRequiredService<GoogleSearchService>();
			var functionCallingkernel = functionCallingBuilder.Build();
			functionCallingkernel.Plugins.AddFromObject(new ResearchAgentFunction(searchService));

			var agent = new ChatCompletionAgent
			{
				Name = "ResearchAgent",
				Instructions = researchAgent,
				InstructionsRole = AuthorRole.System,
				Kernel = functionCallingkernel,
				Description = "A researcher that can acces real-time data on the internet. Also can query recent financial news.",
				Arguments = new KernelArguments(new PromptExecutionSettings()
				{
					FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
				})
			};

			return agent;
		}
	}

	public class ResearchAgentFunction(GoogleSearchService searchService)
	{
		[KernelFunction("query_internet")]
		[Description("Search the internet with a query")]
		public Task<string> QueryInternet(string query)
		{
			return searchService.SearchAsync(query).ContinueWith(task =>
			{
				var results = task.Result;
				if (results.Count == 0)
				{
					return "No results found.";
				}
				var sb = new System.Text.StringBuilder();
				foreach (var result in results)
				{
					sb.AppendLine($"Title: {result.Title}");
					sb.AppendLine($"Link: {result.Link}");
					sb.AppendLine($"Snippet: {result.Snippet}");
					if (!string.IsNullOrEmpty(result.Content))
					{
						sb.AppendLine($"Content: {result.Content.Substring(0, Math.Min(1000, result.Content.Length))}...");
					}
					sb.AppendLine();
				}

				return sb.ToString();
			});
		}

		[KernelFunction("get_stock_price")]
		[Description("Get the stock price for a given subject")]
		public Task<string> GetStockPrice(string subject, string date)
		{
			return Task.FromResult($"The price is $400 for {date}");
		}
	}
}
