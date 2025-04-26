using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using static System.Net.Mime.MediaTypeNames;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM
{
	public class WebLLMChatClient : IWebChatClient
	{
		private readonly IJSRuntime jsRuntime;
		private readonly string modelId;
		private InteropInstance? interopInstance = null;

		public ChatClientMetadata Metadata { get; }

		private IJSObjectReference? module = null;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S4462:Calls to \"async\" methods should not be blocking", Justification = "Constructor")]
		public WebLLMChatClient(IJSRuntime jsRuntime, string modelId)
		{
			this.jsRuntime = jsRuntime;
			this.modelId = modelId;
			Metadata = new(nameof(WebLLMChatClient), defaultModelId: modelId);
		}

		public async Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> chatMessages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			throw new NotSupportedException("Use GetStreamingResponseAsync instead.");
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

					yield return new ChatResponseUpdate(
						ChatRole.Assistant,
						response.Choices?.ElementAtOrDefault(0)?.Delta?.Content ?? string.Empty
						);
				}
				else
				{
					await Task.Delay(100); // Wait for 100ms before checking again
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

	public class AgentManager
	{
		private readonly List<IAgent> agents;

		public AgentManager(IEnumerable<IAgent> agents)
		{
			this.agents = agents.ToList();
		}

		public async Task<string> HandleRequestAsync(string request)
		{
			var requestParserAgent = agents.OfType<RequestParserAgent>().FirstOrDefault();
			if (requestParserAgent == null)
			{
				throw new InvalidOperationException("RequestParserAgent not found.");
			}

			var tasks = await requestParserAgent.ParseRequestAsync(request);
			var results = new List<string>();

			foreach (var task in tasks)
			{
				var agent = agents.FirstOrDefault(a => a.CanHandleTask(task));
				if (agent != null)
				{
					results.Add(await agent.HandleTaskAsync(task));
				}
			}

			var resultAggregatorAgent = agents.OfType<ResultAggregatorAgent>().FirstOrDefault();
			if (resultAggregatorAgent == null)
			{
				throw new InvalidOperationException("ResultAggregatorAgent not found.");
			}

			return await resultAggregatorAgent.AggregateResultsAsync(results);
		}
	}

	public interface IAgent
	{
		bool CanHandleTask(string task);
		Task<string> HandleTaskAsync(string task);
	}

	public class DatabaseQueryAgent : IAgent
	{
		public bool CanHandleTask(string task)
		{
			return task.StartsWith("query database");
		}

		public async Task<string> HandleTaskAsync(string task)
		{
			// Implement database query logic here
			return "Database query result";
		}
	}

	public class PortfolioOptimizationAgent : IAgent
	{
		public bool CanHandleTask(string task)
		{
			return task.StartsWith("optimize portfolio");
		}

		public async Task<string> HandleTaskAsync(string task)
		{
			// Implement portfolio optimization logic here
			return "Portfolio optimization result";
		}
	}

	public class BingQueryAgent : IAgent
	{
		public bool CanHandleTask(string task)
		{
			return task.StartsWith("query bing");
		}

		public async Task<string> HandleTaskAsync(string task)
		{
			// Implement Bing API query logic here
			return "Bing query result";
		}
	}

	public class ResultAggregatorAgent : IAgent
	{
		public bool CanHandleTask(string task)
		{
			return task.StartsWith("aggregate results");
		}

		public async Task<string> HandleTaskAsync(string task)
		{
			// Implement result aggregation logic here
			return "Aggregated results";
		}

		public async Task<string> AggregateResultsAsync(IEnumerable<string> results)
		{
			// Implement result aggregation logic here
			return string.Join(", ", results);
		}
	}

	public class RequestParserAgent : IAgent
	{
		public bool CanHandleTask(string task)
		{
			return task.StartsWith("parse request");
		}

		public async Task<string> HandleTaskAsync(string task)
		{
			// Implement request parsing logic here
			return "Parsed request";
		}

		public async Task<IEnumerable<string>> ParseRequestAsync(string request)
		{
			// Implement request parsing logic here
			return new List<string> { "task1", "task2" };
		}
	}
}
