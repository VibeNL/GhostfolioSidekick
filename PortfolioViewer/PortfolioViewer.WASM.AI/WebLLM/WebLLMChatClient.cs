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
			var module = await LoadJsModuleAsync(jsRuntime, "./js/dist/webllm-interop.js");
			await module.InvokeVoidAsync("initializeLLM");

			OnProgress?.Report(new InitializeProgress(1));
		}

		public static async Task<IJSObjectReference> LoadJsModuleAsync(
	IJSRuntime jsRuntime, string path)
		{
			return await jsRuntime.InvokeAsync<IJSObjectReference>(
				"import", path);
		}
	}
}
