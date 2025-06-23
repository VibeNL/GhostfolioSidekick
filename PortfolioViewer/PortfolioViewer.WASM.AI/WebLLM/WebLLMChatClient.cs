using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM
{
	public partial class WebLLMChatClient : IWebChatClient
	{
		private readonly IJSRuntime jsRuntime;
		private readonly ILogger<WebLLMChatClient> logger;
		private readonly Dictionary<ChatMode, string> modelIds;
		private InteropInstance? interopInstance = null;

		private IJSObjectReference? module = null;

		public ChatMode ChatMode { get; set; } = ChatMode.Chat;

		private const string SystemPromptWithFunctions = """
You are an AI assistant that can answer questions or call functions to get specific information.

You have access to the following functions:

[FUNCTIONS]

If a user asks something that can be answered with a function, use a tool_calls array with per function an id, name and valid JSON arguments. If not, answer normally.
Do not respond with any other text than the tool_calls array when answering with a function.

Format function calls like this:
"tool_calls": [
    {
        "id": "call_abc123",
        "type": "function",
        "function": {
            "name": "OrderPizzaPlugin-add_pizza_to_cart",
            "arguments": "{\n\"size\": \"Medium\",\n\"toppings\": [\"Cheese\", \"Pepperoni\"]\n}"
        }
    }
]
""";

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S4462:Calls to \"async\" methods should not be blocking", Justification = "Constructor")]
		public WebLLMChatClient(IJSRuntime jsRuntime, ILogger<WebLLMChatClient> logger, Dictionary<ChatMode, string> modelIds)
		{
			this.jsRuntime = jsRuntime;
			this.logger = logger;
			this.modelIds = modelIds;
		}

		public async Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			// Call GetStreamingResponseAsync
			var msg = new StringBuilder();
			await foreach (var response in GetStreamingResponseAsync(messages, options, cancellationToken))
			{
				msg.Append(response.Text);
			}

			// If no response was received, return an empty response
			return new ChatResponse(new ChatMessage(ChatRole.Assistant, msg.ToString()));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (interopInstance == null)
			{
				throw new NotSupportedException();
			}

			// If the last message is assistant, fake it to be a user call
			var list = messages.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
			var convertedMessages = list
				.Select((x, i) => i == list.Count - 1 ? Fix(x) : x)
				.Select(x => RemoveThink(x))
				.ToList();

			if (convertedMessages.Count == 0)
			{
				// If no messages are provided, return an empty response
				yield return new ChatResponseUpdate(ChatRole.Assistant, string.Empty);
				yield break;
			}

			// Add function calling messages if the chat mode is FunctionCalling
			if (ChatMode == ChatMode.FunctionCalling && options?.Tools?.Any() == true)
			{
				// Add prompt with function calling instructions
				var functionCalls = string.Join(Environment.NewLine, 
						options.Tools.Select(tool => $"- {{\"name\": \"{tool.Name}\", \"description\": \"{tool.Description}\"}}"));
				convertedMessages.Add(new ChatMessage(ChatRole.User, SystemPromptWithFunctions.Replace("[FUNCTIONS]", functionCalls)));
			}

			var model = modelIds[ChatMode];

			// Call the `completeStreamWebLLM` function in the JavaScript module, but do not wait for it to complete
			_ = Task.Run(async () =>
			{
				await (await GetModule()).InvokeVoidAsync(
									"completeStreamWebLLM",
									interopInstance.ConvertMessage(convertedMessages),
									model,
									ChatMode == ChatMode.ChatWithThinking,
									null
									);
			});

			string totalText = string.Empty;

			while (true)
			{
				// Wait for a response to be available
				if (interopInstance.WebLLMCompletions.TryDequeue(out WebLLMCompletion? response))
				{
					if (response.IsStreamComplete)
					{
						if ((options?.Tools?.Any() ?? false) && !string.IsNullOrWhiteSpace(totalText))
						{
							// Return the final response as a single update
							yield return new ChatResponseUpdate(ChatRole.Assistant, ChatMessageContentHelper.ToDisplayText(totalText));
						}

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

					var content = choice.Delta?.Content;
					totalText += content ?? string.Empty;

					if ((options?.Tools?.Any() ?? false))
					{
						logger.LogDebug("ChatRole.Assistant: {Message}", content);
					}
					else
					{
						logger.LogDebug("ChatRole.Assistant: {Message}", content);
						yield return new ChatResponseUpdate(ChatRole.Assistant, content ?? string.Empty);
					}
				}
				else
				{
					await Task.Delay(1, cancellationToken);
				}
			}
		}

		private static ChatMessage RemoveThink(ChatMessage x)
		{
			return new ChatMessage(x.Role, ChatMessageContentHelper.ToDisplayText(x.Text)) { AuthorName = x.AuthorName };
		}

		private static ChatMessage Fix(ChatMessage x)
		{
			if (x.Role == ChatRole.Assistant)
			{
				return new ChatMessage(ChatRole.User, x.Text) { AuthorName = x.AuthorName };
			}

			return x;
		}

		public object? GetService(Type serviceType, object? serviceKey) => this;

		public TService? GetService<TService>(object? key = null)
			where TService : class => this as TService;

		public void Dispose() { }

		public async Task InitializeAsync(IProgress<InitializeProgress> OnProgress)
		{
			// Call the `initialize` function in the JavaScript module
			interopInstance = new(OnProgress);
			await (await GetModule()).InvokeVoidAsync(
				"initializeWebLLM",
				modelIds.Select(x => x.Value).Distinct(),
				DotNetObjectReference.Create(interopInstance));
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

		public IWebChatClient Clone()
		{
			return new WebLLMChatClient(jsRuntime, logger, modelIds)
			{
				interopInstance = interopInstance,
				module = module,
				ChatMode = this.ChatMode,
			};
		}

		private bool TryParseFunctionCall(string? content, out string functionName, out JsonElement arguments)
		{
			functionName = string.Empty;
			arguments = default;

			if (string.IsNullOrWhiteSpace(content)) return false;

			try
			{
				using var doc = JsonDocument.Parse(content);
				if (doc.RootElement.TryGetProperty("tool_call", out var toolCall))
				{
					functionName = toolCall.GetProperty("name").GetString() ?? "";
					// Clone the arguments element to avoid referencing disposed memory
					arguments = JsonDocument.Parse(toolCall.GetProperty("arguments").GetRawText()).RootElement.Clone();
					return true;
				}
			}
			catch (JsonException ex)
			{
				logger.LogWarning(ex, "Failed to parse potential tool call");
			}

			return false;
		}

		private async Task<string> CallToolAsync(ChatOptions options, string name, JsonElement arguments)
		{
			var tool = options.Tools?.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
			if (tool == null)
			{
				logger.LogWarning("Tool with name '{ToolName}' not found.", name);
				return $"Tool '{name}' not found.";
			}

			throw new NotImplementedException($"Tool '{name}' is not implemented yet. Arguments: {arguments.ToString()}");
		}
	}
}
