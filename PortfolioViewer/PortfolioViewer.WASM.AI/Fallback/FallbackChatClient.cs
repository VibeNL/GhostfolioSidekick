using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Fallback
{
	public class FallbackChatClient : IWebChatClient
	{
		private readonly IWebChatClient primaryClient;
		private readonly IWebChatClient fallbackClient;
		private readonly ILogger<FallbackChatClient> logger;
		private IWebChatClient? currentActiveClient;

		public ChatMode ChatMode 
		{ 
			get => primaryClient.ChatMode;
			set
			{
				primaryClient.ChatMode = value;
				fallbackClient.ChatMode = value;
			}
		}

		public FallbackChatClient(
			IWebChatClient primaryClient, 
			IWebChatClient fallbackClient,
			ILogger<FallbackChatClient> logger)
		{
			this.primaryClient = primaryClient;
			this.fallbackClient = fallbackClient;
			this.logger = logger;
		}

		public async Task InitializeAsync(IProgress<InitializeProgress> OnProgress)
		{
			// Try to initialize the primary client (WebLLM) first
			logger.LogInformation("Initializing primary chat client (WebLLM)...");
			
			var primaryProgress = new Progress<InitializeProgress>(progress =>
			{
				OnProgress.Report(new InitializeProgress(progress.Progress * 0.7, $"Primary: {progress.Message}"));
			});

			await primaryClient.InitializeAsync(primaryProgress);

			// Check if primary client is ready
			if (IsPrimaryClientReady())
			{
				currentActiveClient = primaryClient;
				logger.LogInformation("Primary client (WebLLM) initialized successfully");
				OnProgress.Report(new InitializeProgress(1.0, "WebLLM initialized - GPU acceleration active"));
				return;
			}

			// Primary client failed, try fallback client (LLamaSharp)
			logger.LogWarning("Primary client (WebLLM) failed to initialize, trying fallback client (LLamaSharp)...");
			
			var fallbackProgress = new Progress<InitializeProgress>(progress =>
			{
				OnProgress.Report(new InitializeProgress(0.7 + (progress.Progress * 0.3), $"Fallback: {progress.Message}"));
			});

			await fallbackClient.InitializeAsync(fallbackProgress);

			if (IsFallbackClientReady())
			{
				currentActiveClient = fallbackClient;
				logger.LogInformation("Fallback client (LLamaSharp) initialized successfully");
				OnProgress.Report(new InitializeProgress(1.0, "LLamaSharp CPU fallback initialized"));
			}
			else
			{
				logger.LogError("Both primary and fallback clients failed to initialize");
				OnProgress.Report(new InitializeProgress(0.0, "Error: Both WebLLM and LLamaSharp failed to initialize"));
			}
		}

		private bool IsPrimaryClientReady()
		{
			// Check if WebLLM client is ready
			if (primaryClient is WebLLM.WebLLMChatClient webLLMClient)
			{
				return webLLMClient.IsInitialized && webLLMClient.HasWebGPUSupport;
			}
			return false;
		}

		private bool IsFallbackClientReady()
		{
			// Check if LLamaSharp client is ready
			if (fallbackClient is LLamaSharp.LLamaSharpChatClient llamaClient)
			{
				return llamaClient.IsInitialized;
			}
			return false;
		}

		public async Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (currentActiveClient == null)
			{
				return new ChatResponse(new ChatMessage(ChatRole.Assistant, 
					"No AI client is available. Please check initialization."));
			}

			try
			{
				return await currentActiveClient.GetResponseAsync(messages, options, cancellationToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error in active client, attempting fallback");
				
				// If the primary client fails during operation, try the fallback
				if (currentActiveClient == primaryClient && IsFallbackClientReady())
				{
					logger.LogInformation("Switching to fallback client due to primary client error");
					currentActiveClient = fallbackClient;
					return await fallbackClient.GetResponseAsync(messages, options, cancellationToken);
				}

				// If fallback also fails or is not available, return error
				return new ChatResponse(new ChatMessage(ChatRole.Assistant, 
					$"AI service encountered an error: {ex.Message}"));
			}
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (currentActiveClient == null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, 
					"No AI client is available. Please check initialization.");
				yield break;
			}

			// Try primary client first
			var primaryResults = new List<ChatResponseUpdate>();
			bool primarySucceeded = false;
			
			IAsyncEnumerable<ChatResponseUpdate> primaryStream;
			try
			{
				primaryStream = currentActiveClient.GetStreamingResponseAsync(messages, options, cancellationToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error creating primary stream, attempting fallback");
				primaryStream = CreateErrorStream($"Primary client error: {ex.Message}");
			}

			await foreach (var update in primaryStream)
			{
				if (update.Text?.Contains("Error:") == true && !primarySucceeded && 
				    currentActiveClient == primaryClient && IsFallbackClientReady())
				{
					// Error detected early, try fallback
					logger.LogInformation("Primary client error detected, switching to fallback");
					currentActiveClient = fallbackClient;
					
					await foreach (var fallbackUpdate in fallbackClient.GetStreamingResponseAsync(messages, options, cancellationToken))
					{
						yield return fallbackUpdate;
					}
					yield break;
				}
				
				primarySucceeded = true;
				yield return update;
			}
		}

		private async IAsyncEnumerable<ChatResponseUpdate> CreateErrorStream(string errorMessage)
		{
			yield return new ChatResponseUpdate(ChatRole.Assistant, errorMessage);
		}

		public IWebChatClient Clone()
		{
			return new FallbackChatClient(
				primaryClient.Clone(),
				fallbackClient.Clone(),
				logger)
			{
				currentActiveClient = currentActiveClient?.Clone()
			};
		}

		public object? GetService(Type serviceType, object? serviceKey) => this;

		public TService? GetService<TService>(object? key = null) where TService : class => this as TService;

		public void Dispose()
		{
			primaryClient?.Dispose();
			fallbackClient?.Dispose();
			currentActiveClient?.Dispose();
		}
	}
}