using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Portfolio;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public static class ServiceCollectionExtentions
	{
		private const string modelid = "Qwen3-4B-q4f32_1-MLC";

		public static void AddWebChatClient(this IServiceCollection services)
		{
			services.AddSingleton<ICustomChatClient>((s) => new WebLLMChatClient(
				s.GetRequiredService<IJSRuntime>(),
				s.GetRequiredService<ILogger<WebLLMChatClient>>(),
				new Dictionary<ChatMode, string> {
					{ ChatMode.Chat, modelid },
					{ ChatMode.ChatWithThinking, modelid },
					{ ChatMode.FunctionCalling, modelid },
				}
			));

			// Forward IChatClient to the ICustomChatClient registration so that
			// components depending on the base interface (e.g. ResearchAgentToolProvider) resolve correctly.
			services.AddSingleton<IChatClient>(s => s.GetRequiredService<ICustomChatClient>());

			services.AddSingleton(s => new ModelInfo
			{
				Name = modelid,
				MaxTokens = 4096
			});
		}

		public static void AddPortfolioTools(this IServiceCollection services)
		{
			services.AddSingleton<IAgentToolProvider, PortfolioAgentToolProvider>();
		}
	}
}



