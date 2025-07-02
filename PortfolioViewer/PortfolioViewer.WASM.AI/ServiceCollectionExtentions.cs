using GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.LLamaSharp;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Fallback;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public static class ServiceCollectionExtentions
	{
		private const string modelid = "Qwen3-4B-q4f32_1-MLC";
		
		// Default paths for LLamaSharp models - these should be configured based on your setup
		private static readonly Dictionary<ChatMode, string> LLamaModelPaths = new()
		{
			{ ChatMode.Chat, Path.Combine("models", "llama-2-7b-chat.q4_0.gguf") },
			{ ChatMode.ChatWithThinking, Path.Combine("models", "llama-2-7b-chat.q4_0.gguf") },
			{ ChatMode.FunctionCalling, Path.Combine("models", "llama-2-7b-chat.q4_0.gguf") },
		};

		public static void AddWebChatClient(this IServiceCollection services)
		{
			// Register individual chat clients
			services.AddSingleton<WebLLMChatClient>((s) => new WebLLMChatClient(
				s.GetRequiredService<IJSRuntime>(),
				s.GetRequiredService<ILogger<WebLLMChatClient>>(),
				new Dictionary<ChatMode, string> {
					{ ChatMode.Chat, modelid },
					{ ChatMode.ChatWithThinking, modelid },
					{ ChatMode.FunctionCalling, modelid },
				}
			));

			services.AddSingleton<LLamaSharpChatClient>((s) => new LLamaSharpChatClient(
				s.GetRequiredService<ILogger<LLamaSharpChatClient>>(),
				LLamaModelPaths
			));

			// Register the fallback client as the main IWebChatClient
			services.AddSingleton<IWebChatClient>((s) => new FallbackChatClient(
				s.GetRequiredService<WebLLMChatClient>(),
				s.GetRequiredService<LLamaSharpChatClient>(),
				s.GetRequiredService<ILogger<FallbackChatClient>>()
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

		/// <summary>
		/// Configures the LLamaSharp model paths. Call this method to customize model locations.
		/// </summary>
		/// <param name="services">The service collection</param>
		/// <param name="modelPaths">Dictionary mapping ChatMode to model file paths</param>
		public static void ConfigureLLamaSharpModels(this IServiceCollection services, Dictionary<ChatMode, string> modelPaths)
		{
			// Update the static model paths
			foreach (var kvp in modelPaths)
			{
				LLamaModelPaths[kvp.Key] = kvp.Value;
			}
		}
	}
}


