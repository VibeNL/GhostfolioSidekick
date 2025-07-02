using GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.LLamaSharp;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Fallback;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;

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
			// Register HttpClient for model downloading with explicit configuration
			services.AddHttpClient<ModelDownloadService>("ModelDownloadService", (serviceProvider, client) =>
			{
				// Configure the HttpClient to use the same settings as the default client
				// This should work with service discovery when properly configured
				try
				{
					var configuration = serviceProvider.GetService<IConfiguration>();
					var apiServiceHttp = configuration?.GetSection("Services:apiservice:http").Get<string[]>()?.FirstOrDefault();
					
					if (!string.IsNullOrWhiteSpace(apiServiceHttp))
					{
						client.BaseAddress = new Uri("http://apiservice/");
					}
					else
					{
						 // Use the default HttpClient approach - fallback to host base address
						var defaultClientFactory = serviceProvider.GetService<IHttpClientFactory>();
						if (defaultClientFactory != null)
						{
							try
							{
								var defaultClient = defaultClientFactory.CreateClient(string.Empty);
								if (defaultClient.BaseAddress != null)
								{
									client.BaseAddress = defaultClient.BaseAddress;
								}
								else
								{
									// Final fallback
									client.BaseAddress = new Uri("https://localhost:7042/");
								}
							}
							catch
							{
								// If default client creation fails, use localhost
								client.BaseAddress = new Uri("https://localhost:7042/");
							}
						}
						else
						{
							client.BaseAddress = new Uri("https://localhost:7042/");
						}
					}
				}
				catch (Exception)
				{
					// If any configuration fails, use localhost as fallback
					client.BaseAddress = new Uri("https://localhost:7042/");
				}
				
				// Set appropriate timeout for large downloads
				client.Timeout = TimeSpan.FromMinutes(30); // 30 minutes for 2.4GB download
				
				// Add headers for better compatibility
				client.DefaultRequestHeaders.Add("User-Agent", "PortfolioViewer.WASM/1.0");
			});
			
			// Register the model download service with proper HttpClient injection
			services.AddSingleton<ModelDownloadService>((s) => 
			{
				var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
				var httpClient = httpClientFactory.CreateClient("ModelDownloadService");
				return new ModelDownloadService(
					httpClient,
					s.GetRequiredService<ILogger<ModelDownloadService>>(),
					s.GetRequiredService<IJSRuntime>()
				);
			});

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


