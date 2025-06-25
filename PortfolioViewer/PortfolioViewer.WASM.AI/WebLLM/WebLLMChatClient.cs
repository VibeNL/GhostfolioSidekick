using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using OpenAI.Responses;
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
{ "tool_calls": [
    {
        "id": "call_abc123",
        "type": "function",
        "function": {
            "name": "OrderPizzaPlugin-add_pizza_to_cart",
            "arguments": "{\n\"size\": \"Medium\",\n\"toppings\": [\"Cheese\", \"Pepperoni\"]\n}"
        }
    }
] }
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
				var functions = options.Tools.OfType<KernelFunction>();

				// Add prompt with function calling instructions
				var functionCalls = string.Join(Environment.NewLine,
											functions.Select((tool, i) => 
												new StringBuilder()
													.AppendLine($"{{")
													.AppendLine($"  \"id\": \"call{i}\",")
													.AppendLine($"  \"type\": \"function\",")
													.AppendLine($"  \"function\": {{")
													.AppendLine($"    \"name\": \"{tool.Name}\",")
													.AppendLine($"    \"arguments\": {JsonSerializer.Serialize(tool.Metadata.Parameters)}")
													.AppendLine($"  }}")
													.Append("}")
											));
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
							// Try to parse function calls from the response
							if (TryParseToolCalls(ChatMessageContentHelper.ToDisplayText(totalText), out var toolCalls))
							{
								foreach (var toolCall in toolCalls)
								{
									var output = await CallToolAsync(options, toolCall.Name, JsonSerializer.SerializeToElement(toolCall.Arguments));
									yield return new ChatResponseUpdate(ChatRole.Assistant, output);
								}

								yield break;
							}
							else
							{
								// Return the final response as a single update for function calling
								string content1 = ChatMessageContentHelper.ToDisplayText(totalText).Trim();
								yield return new ChatResponseUpdate(ChatRole.Assistant, content1);
							}
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

		private bool TryParseToolCalls(string content, out List<Microsoft.Extensions.AI.FunctionCallContent> toolCalls)
		{
			toolCalls = [];
			content = content.Trim('\'');
			if (string.IsNullOrWhiteSpace(content))
			{
				return false;
			}

			try
			{
				using var doc = JsonDocument.Parse(content);
				if (doc.RootElement.TryGetProperty("tool_calls", out var toolCallsArray) && toolCallsArray.ValueKind == JsonValueKind.Array)
				{
					foreach (var toolCall in toolCallsArray.EnumerateArray())
					{
						var function = toolCall.GetProperty("function");
						var id = toolCall.GetProperty("id").GetString() ?? Random.Shared.Next().ToString();
						var name = function.GetProperty("name").GetString() ?? string.Empty;
						var arguments = JsonDocument.Parse(function.GetProperty("arguments").GetRawText()).RootElement.Clone();
						var argumentsProperty = function.GetProperty("arguments");
						Dictionary<string, object?>? argumentsDict = null;
						if (argumentsProperty.ValueKind == JsonValueKind.Object)
						{
							argumentsDict = new Dictionary<string, object?>();
							foreach (var prop in argumentsProperty.EnumerateObject())
							{
								argumentsDict[prop.Name] = prop.Value.ValueKind switch
								{
									JsonValueKind.String => prop.Value.GetString(),
									JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDouble(),
									JsonValueKind.True => true,
									JsonValueKind.False => false,
									JsonValueKind.Null => null,
									_ => prop.Value.GetRawText()
								};
							}
						}

						toolCalls.Add(new Microsoft.Extensions.AI.FunctionCallContent(
							id,
							name,
							argumentsDict
						));
					}
					return toolCalls.Count > 0;
				}
			}
			catch (JsonException ex)
			{
				logger.LogWarning(ex, "Failed to parse tool calls from content: {Content}", content);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unexpected error while parsing tool calls from content: {Content}", content);
			}

			return false;
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

		private async Task<string> CallToolAsync(ChatOptions options, string name, JsonElement arguments)
		{
			var tool = options.Tools?.OfType<KernelFunction>()
				.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

			if (tool == null)
			{
				logger.LogWarning("Tool with name '{ToolName}' not found.", name);
				return $"Tool '{name}' not found.";
			}

			// Convert JsonElement arguments to KernelArguments
			var kernelArgs = new KernelArguments();
			if (arguments.ValueKind == JsonValueKind.Object)
			{
				foreach (var prop in arguments.EnumerateObject())
				{
					kernelArgs.Add(prop.Name, prop.Value.ToString());
				}
			}

			// Call the tool (function)
			var result = await tool.InvokeAsync(kernelArgs);
			var output = result?.ToString() ?? "[Function returned null]";
			logger.LogInformation("Tool '{ToolName}' executed with output: {Output}", name, output);
			return output;
		}
	}
}
