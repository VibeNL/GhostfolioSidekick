using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Castle.Core.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using static System.Net.Mime.MediaTypeNames;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM
{
	public class WebLLMChatClient : IWebChatClient
	{
		private readonly IJSRuntime jsRuntime;
		private readonly ILogger<WebLLMChatClient> logger;
		private readonly string modelId;
		private InteropInstance? interopInstance = null;

		public ChatClientMetadata Metadata { get; }

		private IJSObjectReference? module = null;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S4462:Calls to \"async\" methods should not be blocking", Justification = "Constructor")]
		public WebLLMChatClient(IJSRuntime jsRuntime, ILogger<WebLLMChatClient> logger, string modelId)
		{
			this.jsRuntime = jsRuntime;
			this.logger = logger;
			this.modelId = modelId;
			Metadata = new(nameof(WebLLMChatClient), defaultModelId: modelId);
		}

		public async Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> chatMessages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			// Call GetStreamingResponseAsync
			var msg = new StringBuilder();
			await foreach (var response in GetStreamingResponseAsync(chatMessages, options, cancellationToken))
			{
				msg.Append(response.Text);
			}

			// If no response was received, return an empty response
			return new ChatResponse(new ChatMessage(ChatRole.Assistant, msg.ToString()));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> chatMessages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (interopInstance == null)
			{
				throw new NotSupportedException();
			}

			// Call the `initialize` function in the JavaScript module, but do not wait for it to complete
			_ = Task.Run(async () => await (await GetModule()).InvokeVoidAsync("completeStreamWebLLM", interopInstance.ConvertMessage(chatMessages)));

			while (true)
			{
				// Wait for a response to be available
				if (interopInstance.WebLLMCompletions.TryDequeue(out WebLLMCompletion? response))
				{
					if (response.IsStreamComplete)
					{
						yield break;
					}

					if (response.Choices is null || response.Choices.Length == 0)
					{
						continue;
					}

					var choice = response.Choices[0];
					if (choice.Delta is null)
					{
						continue;
					}

					logger.LogDebug("ChatRole.Assistant: {Message}", response.Choices?.ElementAtOrDefault(0)?.Delta?.Content ?? string.Empty);
					yield return new ChatResponseUpdate(
						ChatRole.Assistant,
						response.Choices?.ElementAtOrDefault(0)?.Delta?.Content ?? string.Empty
						);
				}else
				{
					await Task.Delay(1, cancellationToken);
				}
			}
		}

		public object? GetService(Type serviceType, object? serviceKey) => this;

		public TService? GetService<TService>(object? key = null)
			where TService : class => this as TService;

		public void Dispose() { }

		public async Task InitializeAsync(IProgress<InitializeProgress> OnProgress)
		{
			// Call the `initialize` function in the JavaScript module
			interopInstance = new(OnProgress);
			await (await GetModule()).InvokeVoidAsync("initializeWebLLM", modelId, DotNetObjectReference.Create(interopInstance));
		}

		public static async Task<IJSObjectReference> LoadJsModuleAsync(IJSRuntime jsRuntime, string path)
		{
			return await jsRuntime.InvokeAsync<IJSObjectReference>(
				"import", path);
		}

		private async Task<IJSObjectReference> GetModule()
		{
			if (interopInstance == null)
			{
				throw new NotSupportedException("Interop instance is not initialized.");
			}

			module = await LoadJsModuleAsync(jsRuntime, "./js/dist/webllm.interop.js");
			if (module == null)
			{
				throw new NotSupportedException("Module is not initialized.");
			}

			return module;
		}

		public class InteropInstance
		{
			private readonly IProgress<InitializeProgress> _progress;

			public ConcurrentQueue<WebLLMCompletion> WebLLMCompletions { get; init; } = new();

			public InteropInstance(IProgress<InitializeProgress> progress)
			{
				_progress = progress;
			}

			[JSInvokable]
			public void ReportProgress(InitProgressReport progress)
			{
				ArgumentNullException.ThrowIfNull(progress);

				var progressPercent = Math.Min(progress.Progress, 0.99);
				// only report done when text: Finish loading on WebGPU
				if (progress.Text.StartsWith("Finish loading on WebGPU"))
				{
					progressPercent = 1.0;
				}

				_progress.Report(new InitializeProgress(progressPercent, progress.Text));
			}

			[JSInvokable]
			public void ReceiveChunkCompletion(WebLLMCompletion response)
			{
				ArgumentNullException.ThrowIfNull(response);

				// Add the response to the queue
				WebLLMCompletions.Enqueue(response);
			}

			internal IEnumerable<Message> ConvertMessage(IEnumerable<ChatMessage> chatMessages)
			{
				return chatMessages.Select(chatMessage =>
				{
					if (chatMessage.Role == ChatRole.User)
					{
						return new Message("user", chatMessage.Text);
					}
					else if (chatMessage.Role == ChatRole.Assistant)
					{
						return new Message("assistant", chatMessage.Text);
					}
					else if (chatMessage.Role == ChatRole.System)
					{
						return new Message("system", chatMessage.Text);
					}
					else
					{
						throw new NotSupportedException($"Chat role {chatMessage.Role} is not supported.");
					}
				});
			}
		}

		// A progress report for the initialization process
		public record InitProgressReport(double Progress, string Text, double timeElapsed);

		// A chat message
		public record Message(string Role, string Content);

		// A partial chat message
		public record Delta(string Role, string Content);
		// Chat message "cost"
		public record Usage(double CompletionTokens, double PromptTokens, double TotalTokens);
		// A collection of partial chat messages
		public record Choice(int Index, Message? Delta, string Logprobs, string FinishReason);

		// A chat completion response
		public record WebLLMCompletion(
			string Id,
			string Object,
			string Model,
			string SystemFingerprint,
			Choice[]? Choices,
			Usage? Usage
		)
		{
			// The final part of a chat message stream will include Usage
			public bool IsStreamComplete => Usage is not null;
		}
	}
}
