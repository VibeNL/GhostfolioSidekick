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
		private const string researchAgent = @"
					You are ResearchAgent AI — a smart financial assistant. 
					You may call functions if needed";
		public static ChatCompletionAgent Create(IWebChatClient webChatClient)
		{
			IKernelBuilder functionCallingBuilder = Kernel.CreateBuilder();
			functionCallingBuilder.Services.AddScoped<IChatCompletionService>((s) =>
			{
				var client = webChatClient.Clone();
				client.ChatMode = ChatMode.FunctionCalling;
				return client.AsChatCompletionService();
			});
			var functionCallingkernel = functionCallingBuilder.Build();
			functionCallingkernel.Plugins.AddFromType<ResearchAgentFunction>();

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

	public class ResearchAgentFunction
	{
		[KernelFunction("get_financial_news")]
		[Description("Get financial news for a given subject")]
		public Task<string> GetFinancialNews(string subject)
		{
			return Task.FromResult("Its going down");
		}

		[KernelFunction("get_stock_price")]
		[Description("Get the stock price for a given subject")]
		public Task<string> GetStockPrice(string subject, string date )
		{
			return Task.FromResult("The price is $400");
		}
	}
}
