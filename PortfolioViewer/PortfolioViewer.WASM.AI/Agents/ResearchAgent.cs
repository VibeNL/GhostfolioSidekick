using GhostfolioSidekick.ExternalDataProvider.DuckDuckGo;
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
			var duckDuckGoService = serviceProvider.GetRequiredService<DuckDuckGoService>();
			functionCallingBuilder.Services.AddSingleton<DuckDuckGoService>((s) => serviceProvider.GetRequiredService<DuckDuckGoService>());
			var functionCallingkernel = functionCallingBuilder.Build();
			functionCallingkernel.Plugins.AddFromObject(new ResearchAgentFunction(duckDuckGoService));

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

	public class ResearchAgentFunction(DuckDuckGoService duckDuckGoService)
	{
		[KernelFunction("get_financial_news")]
		[Description("Get financial news for a given subject")]
		public Task<string> GetFinancialNews(string subject)
		{
			return duckDuckGoService.SearchAsync(subject).ContinueWith(task =>
			{
				var result = task.Result;
				if (result == null)
				{
					return "No financial news found.";
				}

				var news = new System.Text.StringBuilder();
				news.AppendLine($"**{result.Heading}**");
				news.AppendLine(result.Abstract ?? "No abstract available.");
				news.AppendLine($"[Read more]({result.Redirect})");
				return news.ToString();
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
