using GhostfolioSidekick.ExternalDataProvider.DuckDuckGo;
using GhostfolioSidekick.ExternalDataProvider.Google;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public static class ServiceCollectionExtentions
	{
		public static void AddWebChatClient(this IServiceCollection services)
		{
			services.AddSingleton<IWebChatClient>((s) => new WebLLMChatClient(
				s.GetRequiredService<IJSRuntime>(),
				s.GetRequiredService<ILogger<WebLLMChatClient>>(),
				new Dictionary<ChatMode, string> {
					{ ChatMode.Chat, "Qwen3-4B-q4f32_1-MLC" },
					{ ChatMode.ChatWithThinking, "Qwen3-4B-q4f32_1-MLC" },
					{ ChatMode.FunctionCalling, "Qwen3-4B-q4f32_1-MLC" },
				}
			));

			services.AddSingleton<AgentLogger>();
			services.AddSingleton<AgentOrchestrator>();
			services.AddSingleton<DuckDuckGoService>();
			services.AddHttpClient<GoogleSearchService>();
			services.AddSingleton<GoogleSearchService>((s) =>
			{
				var httpClient = s.GetRequiredService<HttpClient>();
				var apiKey = "AIzaSyCfoFMnKB4igV7eX2M9cNHB9Egi6TY3Pg0";
				var cx = "67916343ce9fd4bfe";
				return new GoogleSearchService(httpClient, apiKey, cx);
			});
		}
	}
}


