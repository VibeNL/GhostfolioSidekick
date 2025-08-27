using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Wllama;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public class FallbackChatClient : IWebChatClient
	{
		private readonly IJSRuntime jsRuntime;
		private readonly ILoggerFactory loggerFactory;
		private readonly ILogger<FallbackChatClient> logger;
		private readonly Dictionary<ChatMode, string> webLlmModelIds;
		private readonly string wllamaModelUrl;
		
		private IWebChatClient? activeClient;
		private bool webLlmFailed = false;

		public ChatMode ChatMode { get; set; } = ChatMode.Chat;

		public FallbackChatClient(
			IJSRuntime jsRuntime,
			ILoggerFactory loggerFactory,
			Dictionary<ChatMode, string> webLlmModelIds,
			string wllamaModelUrl)
		{
			this.jsRuntime = jsRuntime;
			this.loggerFactory = loggerFactory;
			this.logger = loggerFactory.CreateLogger<FallbackChatClient>();
			this.webLlmModelIds = webLlmModelIds;
			this.wllamaModelUrl = wllamaModelUrl;
		}

		public async Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			var client = await GetActiveClientAsync();
			return await client.GetResponseAsync(messages, options, cancellationToken);
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var client = await GetActiveClientAsync();
			await foreach (var update in client.GetStreamingResponseAsync(messages, options, cancellationToken))
			{
				yield return update;
			}
		}

		public object? GetService(Type serviceType, object? serviceKey)
		{
			return activeClient?.GetService(serviceType, serviceKey) ?? this;
		}

		public TService? GetService<TService>(object? key = null) where TService : class
		{
			return activeClient?.GetService<TService>(key) ?? this as TService;
		}

		public void Dispose()
		{
			activeClient?.Dispose();
		}

		public async Task InitializeAsync(IProgress<InitializeProgress> OnProgress)
		{
			if (!webLlmFailed)
			{
				try
				{
					// Try to initialize WebLLM first
					logger.LogInformation("Attempting to initialize WebLLM...");
					
					var webLlmClient = new WebLLMChatClient(jsRuntime, 
						loggerFactory.CreateLogger<WebLLMChatClient>(), 
						webLlmModelIds)
					{
						ChatMode = this.ChatMode
					};
					
					await webLlmClient.InitializeAsync(OnProgress);
					activeClient = webLlmClient;
					
					logger.LogInformation("WebLLM initialized successfully");
					return;
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "WebLLM initialization failed, falling back to Wllama");
					webLlmFailed = true;
				}
			}

			// Fallback to Wllama
			try
			{
				logger.LogInformation("Initializing Wllama fallback...");
				
				var wllamaClient = new WllamaChatClient(jsRuntime,
					loggerFactory.CreateLogger<WllamaChatClient>(),
					wllamaModelUrl)
				{
					ChatMode = this.ChatMode
				};
				
				await wllamaClient.InitializeAsync(OnProgress);
				activeClient = wllamaClient;
				
				logger.LogInformation("Wllama initialized successfully");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Both WebLLM and Wllama initialization failed");
				throw new InvalidOperationException("Failed to initialize any AI client", ex);
			}
		}

		public IWebChatClient Clone()
		{
			var clone = new FallbackChatClient(jsRuntime, loggerFactory, webLlmModelIds, wllamaModelUrl)
			{
				ChatMode = this.ChatMode,
				webLlmFailed = this.webLlmFailed,
				activeClient = this.activeClient?.Clone()
			};
			return clone;
		}

		private async Task<IWebChatClient> GetActiveClientAsync()
		{
			if (activeClient == null)
			{
				throw new InvalidOperationException("Client not initialized. Call InitializeAsync first.");
			}

			// Check if WebLLM client has failed and we need to switch to Wllama
			if (!webLlmFailed && activeClient is WebLLMChatClient)
			{
				try
				{
					// Test if WebLLM is still working by checking if it can handle a simple request
					// This is a lightweight check - we could make it more sophisticated
					return activeClient;
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "WebLLM client failed during operation, switching to Wllama");
					webLlmFailed = true;
					
					// Initialize Wllama as fallback
					var wllamaClient = new WllamaChatClient(jsRuntime,
						loggerFactory.CreateLogger<WllamaChatClient>(),
						wllamaModelUrl)
					{
						ChatMode = this.ChatMode
					};
					
					await wllamaClient.InitializeAsync(new Progress<InitializeProgress>());
					activeClient = wllamaClient;
				}
			}

			return activeClient;
		}
	}
}