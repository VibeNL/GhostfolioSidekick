using GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public static class ServiceCollectionExtentions
	{
		private const string modelid = "Qwen3-4B-q4f32_1-MLC";
		private const string wllamaModelUrl = "https://huggingface.co/ngxson/tinyllama_v0/resolve/main/tinyllama-1.1b-chat-v0.3.Q5_K_M.gguf";

		public static void AddWebChatClient(this IServiceCollection services)
		{
			services.AddSingleton<IWebChatClient>((s) => new FallbackChatClient(
				s.GetRequiredService<IJSRuntime>(),
				s.GetRequiredService<ILoggerFactory>(),
				new Dictionary<ChatMode, string> {
					{ ChatMode.Chat, modelid },
					{ ChatMode.ChatWithThinking, modelid },
					{ ChatMode.FunctionCalling, modelid },
				},
				wllamaModelUrl
			));

			services.AddSingleton<AgentLogger>();
			services.AddSingleton<AgentOrchestrator>();
			
			// Register Google Search service with MCP pattern
			services.AddHttpClient<GoogleSearchService>();
			services.AddSingleton<IGoogleSearchProtocol, GoogleSearchService>();
			services.AddSingleton<GoogleSearchService>((s) =>
			{
				var httpClient = s.GetRequiredService<HttpClient>();
				// Create a context for the GoogleSearchService
				var context = new GoogleSearchContext
				{
					HttpClient = httpClient,
					// Default URLs are already set in the context class
				};
				// Return the service with the context
				return new GoogleSearchService(context);
			});
		}
	}
}


