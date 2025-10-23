using GhostfolioSidekick.AI.Common;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM
{
	[method: System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S4462:Calls to \"async\" methods should not be blocking", Justification = "Constructor")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly", Justification = "<Pending>")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "<Pending>")]
	public partial class WebLLMChatClient(IJSRuntime jsRuntime, ILogger<WebLLMChatClient> logger, Dictionary<ChatMode, string> modelIds) : ICustomChatClient
	{
		private InteropInstance? interopInstance;

		private IJSObjectReference? module;

		public ChatMode ChatMode { get; set; } = ChatMode.Chat;

		private const string SystemPromptWithFunctions = """
You are an AI assistant that can answer questions or call functions to get specific information.

You have access to the following functions:

[FUNCTIONS]

If a user asks something that can be answered with a function, use a tool_calls array with per function an id, name and valid JSON arguments. If not, answer normally.
Do not respond with any other text than the tool_calls array when answering with a function.

IMPORTANT: Make sure the "arguments" value is a JSON-encoded STRING, not a JSON object directly. 
Use the format: "arguments": "{\"parameter1\": \"value\", \"parameter2\": true}"

Format function calls like this:
{ "tool_calls": [
    {
        "id": "call_abc123",
        "type": "function",
        "function": {
            "name": "MyFunction",
            "arguments": "{\"parameter1\": \"It is going up\",\"parameter2\": \"true\"}"
        }
    }
] }
""";

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
			ValidateInteropInstance();

			var convertedMessages = PrepareMessages(messages, options);
			if (IsEmptyMessageList(convertedMessages))
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, string.Empty);
				yield break;
			}

			StartStreamingAsync(convertedMessages, cancellationToken);

			await foreach (var update in ProcessStreamingResponseAsync(options, cancellationToken))
			{
				yield return update;
			}
		}

		private void ValidateInteropInstance()
		{
			if (interopInstance == null)
			{
				throw new NotSupportedException();
			}
		}

		private static bool IsEmptyMessageList(List<ChatMessage> messages)
		{
			return messages.Count == 0;
		}

		private List<ChatMessage> PrepareMessages(IEnumerable<ChatMessage> messages, ChatOptions? options)
		{
			var list = messages.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
			var convertedMessages = list
				.Select((x, i) => i == list.Count - 1 ? Fix(x) : x)
				.Select(RemoveThink)
				.ToList();

			if (ShouldAddFunctionPrompt(options))
			{
				convertedMessages.Add(CreateFunctionPromptMessage(options!));
			}

			return convertedMessages;
		}

		private bool ShouldAddFunctionPrompt(ChatOptions? options)
		{
			return ChatMode == ChatMode.FunctionCalling && options?.Tools?.Any() == true;
		}

		private static ChatMessage CreateFunctionPromptMessage(ChatOptions options)
		{
			var functions = options.Tools!.OfType<KernelFunction>().ToList();
			var functionDefinitions = BuildFunctionDefinitions(functions);
			return new ChatMessage(ChatRole.User, SystemPromptWithFunctions.Replace("[FUNCTIONS]", functionDefinitions));
		}

		private static string BuildFunctionDefinitions(List<KernelFunction> functions)
		{
			var functionDefinitions = new StringBuilder();
			foreach (var function in functions)
			{
				functionDefinitions.AppendLine($"- {function.Name}");
				functionDefinitions.AppendLine($"  Description: {function.Description ?? function.Name}");
				functionDefinitions.AppendLine("  Parameters:");

				foreach (var param in function.Metadata.Parameters)
				{
					var paramDesc = param.Description ?? param.Name;
					var paramType = param.ParameterType?.Name ?? "string";
					functionDefinitions.AppendLine($"    - {param.Name} ({paramType}): {paramDesc}");
				}
				functionDefinitions.AppendLine();
			}
			return functionDefinitions.ToString();
		}

		private void StartStreamingAsync(List<ChatMessage> convertedMessages, CancellationToken cancellationToken)
		{
			var model = modelIds[ChatMode];
			_ = Task.Run(async () =>
			{
				await (await GetModule()).InvokeVoidAsync(
					"completeStreamWebLLM",
					InteropInstance.ConvertMessage(convertedMessages),
					model,
					ChatMode == ChatMode.ChatWithThinking,
					null
				);
			}, cancellationToken);
		}

		private async IAsyncEnumerable<ChatResponseUpdate> ProcessStreamingResponseAsync(
			ChatOptions? options,
			[EnumeratorCancellation] CancellationToken cancellationToken)
		{
			var totalTextBuilder = new StringBuilder();

			while (true)
			{
				var response = TryDequeueResponse();
				if (response == null)
				{
					await Task.Delay(1, cancellationToken);
					continue;
				}

				if (response.IsStreamComplete)
				{
					await foreach (var finalUpdate in HandleStreamComplete(options, totalTextBuilder))
					{
						yield return finalUpdate;
					}
					yield break;
				}

				var content = ExtractContentFromResponse(response);
				if (content != null)
				{
					totalTextBuilder.Append(content);
					yield return CreateResponseUpdate(options, content);
				}
			}
		}

		private WebLLMCompletion? TryDequeueResponse()
		{
			return interopInstance!.WebLLMCompletions.TryDequeue(out WebLLMCompletion? response)
				? response
				: null;
		}

		private async IAsyncEnumerable<ChatResponseUpdate> HandleStreamComplete(
			ChatOptions? options,
			StringBuilder totalTextBuilder)
		{
			if (!HasTools(options) || totalTextBuilder.Length == 0)
			{
				yield break;
			}

			var totalText = totalTextBuilder.ToString();
			if (TryParseToolCalls(ChatMessageContentHelper.ToDisplayText(totalText), out var toolCalls))
			{
				await foreach (var update in ExecuteToolCallsAsync(options!, toolCalls))
				{
					yield return update;
				}
			}
			else
			{
				var content = ChatMessageContentHelper.ToDisplayText(totalText).Trim();
				yield return new ChatResponseUpdate(ChatRole.Assistant, content);
			}
		}

		private async IAsyncEnumerable<ChatResponseUpdate> ExecuteToolCallsAsync(
			ChatOptions options,
			List<Microsoft.Extensions.AI.FunctionCallContent> toolCalls)
		{
			foreach (var toolCall in toolCalls)
			{
				var output = await CallToolAsync(options, toolCall.Name, toolCall.Arguments);
				yield return new ChatResponseUpdate(ChatRole.Assistant, output);
			}
		}

		private static string? ExtractContentFromResponse(WebLLMCompletion response)
		{
			if (response.Choices?.Length > 0)
			{
				return response.Choices[0].Delta?.Content;
			}
			return null;
		}

		private ChatResponseUpdate CreateResponseUpdate(ChatOptions? options, string content)
		{
			var role = HasTools(options) ? ChatRole.Tool : ChatRole.Assistant;
			logger.LogDebug("{Role}: {Message}", role, content);
			return new ChatResponseUpdate(role, content);
		}

		private static bool HasTools(ChatOptions? options)
		{
			return options?.Tools?.Any() ?? false;
		}

		private bool TryParseToolCalls(string content, out List<Microsoft.Extensions.AI.FunctionCallContent> toolCalls)
		{
			toolCalls = [];

			if (string.IsNullOrWhiteSpace(content))
			{
				return false;
			}

			content = content.Trim('\'');

			try
			{
				using var doc = JsonDocument.Parse(content);
				return TryExtractToolCallsFromJson(doc, out toolCalls);
			}
			catch (JsonException ex)
			{
				logger.LogWarning(ex, "Failed to parse tool calls from content: {Content}", content);
				return false;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unexpected error while parsing tool calls from content: {Content}", content);
				return false;
			}
		}

		private bool TryExtractToolCallsFromJson(JsonDocument doc, out List<Microsoft.Extensions.AI.FunctionCallContent> toolCalls)
		{
			toolCalls = [];

			if (!doc.RootElement.TryGetProperty("tool_calls", out var toolCallsArray) ||
				toolCallsArray.ValueKind != JsonValueKind.Array)
			{
				return false;
			}

			foreach (var toolCall in toolCallsArray.EnumerateArray())
			{
				var functionCallContent = CreateFunctionCallContent(toolCall);
				if (functionCallContent != null)
				{
					toolCalls.Add(functionCallContent);
				}
			}

			return toolCalls.Count > 0;
		}

		private Microsoft.Extensions.AI.FunctionCallContent? CreateFunctionCallContent(JsonElement toolCall)
		{
			try
			{
				var function = toolCall.GetProperty("function");
				var id = toolCall.GetProperty("id").GetString() ?? Random.Shared.Next().ToString();
				var name = function.GetProperty("name").GetString() ?? string.Empty;
				var argumentsDict = ExtractFunctionArguments(function);

				return new Microsoft.Extensions.AI.FunctionCallContent(id, name, argumentsDict ?? []);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to create function call content from tool call");
				return null;
			}
		}

		private Dictionary<string, object?>? ExtractFunctionArguments(JsonElement function)
		{
			if (!function.TryGetProperty("arguments", out var argumentsProperty))
			{
				return [];
			}

			return argumentsProperty.ValueKind switch
			{
				JsonValueKind.String => ParseArgumentsFromString(argumentsProperty.GetString()),
				JsonValueKind.Object => ParseArgumentsFromObject(argumentsProperty),
				_ => []
			};
		}

		private Dictionary<string, object?>? ParseArgumentsFromString(string? argumentsStr)
		{
			if (string.IsNullOrEmpty(argumentsStr))
			{
				return [];
			}

			try
			{
				using var argDoc = JsonDocument.Parse(argumentsStr);
				return ConvertJsonElementToDictionary(argDoc.RootElement);
			}
			catch (JsonException ex)
			{
				logger.LogWarning(ex, "Failed to parse function arguments as JSON string: {Arguments}", argumentsStr);
				return new Dictionary<string, object?> { { "value", argumentsStr } };
			}
		}

		private static Dictionary<string, object?> ParseArgumentsFromObject(JsonElement argumentsProperty)
		{
			return ConvertJsonElementToDictionary(argumentsProperty);
		}

		private static Dictionary<string, object?> ConvertJsonElementToDictionary(JsonElement element)
		{
			var argumentsDict = new Dictionary<string, object?>();
			foreach (var prop in element.EnumerateObject())
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
			return argumentsDict;
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

		public object? GetService(Type serviceType, object? serviceKey = null) => this;

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

		public ICustomChatClient Clone()
		{
			return new WebLLMChatClient(jsRuntime, logger, modelIds)
			{
				interopInstance = interopInstance,
				module = module,
				ChatMode = ChatMode,
			};
		}

		private async Task<string> CallToolAsync(ChatOptions options, string name, IDictionary<string, object?>? arguments)
		{
			var tool = options.Tools?.OfType<KernelFunction>()
				.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

			if (tool == null)
			{
				logger.LogWarning("Tool with name '{ToolName}' not found.", name);
				return $"Tool '{name}' not found.";
			}

			// Convert arguments to KernelArguments
			var kernelArgs = new KernelArguments();
			if (arguments != null)
			{
				foreach (var arg in arguments)
				{
					kernelArgs[arg.Key] = arg.Value;
				}
			}

			// Call the tool (function)
			try
			{
				var result = await tool.InvokeAsync(kernelArgs);
				var output = result?.ToString() ?? "[Function returned null]";
				logger.LogInformation("Tool '{ToolName}' executed with output: {Output}", name, output);
				return output;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error executing tool '{ToolName}' with arguments: {Arguments}", name, JsonSerializer.Serialize(arguments));
				return $"Error executing tool '{name}': {ex.Message}";
			}
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}
}
