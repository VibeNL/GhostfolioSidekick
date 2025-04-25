using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM
{
	public class WebLLMChatClient(IJSRuntime jsRuntime, string modelId) : IWebChatClient
	{
		public ChatClientMetadata Metadata { get; } = new(nameof(WebLLMChatClient), defaultModelId: modelId);

		public async Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> chatMessages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			// Simulate some operation.
			await Task.Delay(300, cancellationToken);

			// Return a sample chat completion response randomly.
			string[] responses =
			[
				"This is the first sample response.",
			"Here is another example of a response message.",
			"This is yet another response message."
			];

			return new(new ChatMessage(
				ChatRole.Assistant,
				responses[Random.Shared.Next(responses.Length)]
				));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> chatMessages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			// Simulate streaming by yielding messages one by one.
			string[] words = ["This ", "is ", "the ", "response ", "for ", "the ", "request."];
			foreach (string word in words)
			{
				// Simulate some operation.
				await Task.Delay(100, cancellationToken);

				// Yield the next message in the response.
				yield return new ChatResponseUpdate(ChatRole.Assistant, word);
			}
		}

		public object? GetService(Type serviceType, object? serviceKey) => this;

		public TService? GetService<TService>(object? key = null)
			where TService : class => this as TService;

		public void Dispose() { }

        public async Task InitializeAsync(IProgress<InitializeProgress> OnProgress)
        {
            var module = await LoadJsModuleAsync(jsRuntime, "./js/dist/webllm.interop.js");
            // Call the `initialize` function in the JavaScript module
            await module.InvokeVoidAsync("initializeWebLLM", modelId, DotNetObjectReference.Create(new InteropInstance(OnProgress)));
        }

		public static async Task<IJSObjectReference> LoadJsModuleAsync(
	IJSRuntime jsRuntime, string path)
		{
			return await jsRuntime.InvokeAsync<IJSObjectReference>(
				"import", path);
		}

		public class InteropInstance
		{
			private readonly IProgress<InitializeProgress> _progress;

			public InteropInstance(IProgress<InitializeProgress> progress)
			{
				_progress = progress;
			}

			[JSInvokable]
			public void ReportProgress(InitProgressReport progress)
			{
				if (progress is null)
				{
					throw new ArgumentNullException(nameof(progress));
				}

				var progressPercent = Math.Min(progress.Progress, 0.99);
				// only report done when text: Finish loading on WebGPU
				if (progress.Text.StartsWith("Finish loading on WebGPU"))
				{
					progressPercent = 1.0;
				}

				_progress.Report(new InitializeProgress(progressPercent, progress.Text));
			}
		}

		public record InitProgressReport(double Progress, string Text, double timeElapsed);		
	}
}
