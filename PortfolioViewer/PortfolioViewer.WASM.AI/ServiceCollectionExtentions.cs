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
		
		// Default paths for LLamaSharp models - now using Phi-3 Mini
		private static readonly Dictionary<ChatMode, string> LLamaModelPaths = 
			ModelDownloadService.GetDefaultModelPaths("wwwroot/models");

		public static void AddWebChatClient(this IServiceCollection services)
		{
			// Register HttpClient for model downloading
			services.AddHttpClient<ModelDownloadService>();
			
			// Register the model download service
			services.AddSingleton<ModelDownloadService>();

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
				LLamaModelPaths,
				s.GetRequiredService<ModelDownloadService>()
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

		/// <summary>
		/// Configures the LLamaSharp model to use Phi-3 Mini with automatic downloading.
		/// This is the default configuration, but you can call this method to explicitly set it up.
		/// </summary>
		/// <param name="services">The service collection</param>
		/// <param name="modelsDirectory">Directory where models should be stored (default: "wwwroot/models")</param>
		public static void UsePhi3MiniWithAutoDownload(this IServiceCollection services, string modelsDirectory = "wwwroot/models")
		{
			var phi3ModelPaths = ModelDownloadService.GetDefaultModelPaths(modelsDirectory);
			services.ConfigureLLamaSharpModels(phi3ModelPaths);
		}

		/// <summary>
		/// Disables automatic model downloading. Models must be manually placed in the configured paths.
		/// </summary>
		/// <param name="services">The service collection</param>
		public static void DisableAutoDownload(this IServiceCollection services)
		{
			// Remove the ModelDownloadService registration
			services.AddSingleton<LLamaSharpChatClient>((s) => new LLamaSharpChatClient(
				s.GetRequiredService<ILogger<LLamaSharpChatClient>>(),
				LLamaModelPaths,
				downloadService: null // No download service
			));
		}
	}
}


