using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class WebLLMService
	{
		private string selectedModel = "Phi-3-mini-4k-instruct-q4f16_1-MLC";//"Llama-3.2-1B-Instruct-q4f16_1-MLC";

		private readonly Lazy<Task<IJSObjectReference>> moduleTask;

		private const string ModulePath = "./js/webllm-interop.js";

		public WebLLMService(IJSRuntime jsRuntime)
		{
			moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
				"import", $"{ModulePath}").AsTask());
		}

		public event Action<InitProgress>? OnInitializingChanged;

		public async Task InitializeAsync()
		{
			var module = await moduleTask.Value;
			await module.InvokeVoidAsync("initialize", selectedModel, DotNetObjectReference.Create(this));
			// Calls webllm-interop.js    initialize  (selectedModel, dotnet                            ) 
		}

		// Called from JavaScript
		// dotnetInstance.invokeMethodAsync("OnInitializing", initProgress);
		[JSInvokable]
		public Task OnInitializing(InitProgress status)
		{
			OnInitializingChanged?.Invoke(status);
			return Task.CompletedTask;
		}

		public async Task CompleteStreamAsync(IList<Message> messages)
		{
			var module = await moduleTask.Value;
			await module.InvokeVoidAsync("completeStream", messages);
		}

		public event Func<WebLLMCompletion, Task>? OnChunkCompletion;

		[JSInvokable]
		public Task ReceiveChunkCompletion(WebLLMCompletion response)
		{
			OnChunkCompletion?.Invoke(response);
			return Task.CompletedTask;
		}

	}
}
