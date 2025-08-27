using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Wllama;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Wllama
{
	public partial class WllamaChatClient : IWebChatClient
	{
		private readonly IJSRuntime jsRuntime;
		private readonly ILogger<WllamaChatClient> logger;
		private readonly string modelUrl;
		private WllamaInteropInstance? interopInstance = null;
		private IJSObjectReference? module = null;

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

		public WllamaChatClient(IJSRuntime jsRuntime, ILogger<WllamaChatClient> logger, string modelUrl)
		{
			this.jsRuntime = jsRuntime;
			this.logger = logger;
			this.modelUrl = modelUrl;
		}

		public async Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			var msg = new StringBuilder();
			await foreach (var response in GetStreamingResponseAsync(messages, options, cancellationToken))
			{
				msg.Append(response.Text);
			}

			return new ChatResponse(new ChatMessage(ChatRole.Assistant, msg.ToString()));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (interopInstance == null)
			{
				throw new NotSupportedException("Wllama client is not initialized");
			}

			var list = messages.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToList();
			var convertedMessages = list
				.Select((x, i) => i == list.Count - 1 ? Fix(x) : x)
				.Select(x => RemoveThink(x))
				.ToList();

			if (convertedMessages.Count == 0)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, string.Empty);
				yield break;
			}

			// Add function calling messages if the chat mode is FunctionCalling
			if (ChatMode == ChatMode.FunctionCalling && options?.Tools?.Any() == true)
			{
				var functions = options.Tools.OfType<KernelFunction>().ToList();
				if (functions.Count != 0)
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

					convertedMessages.Add(new ChatMessage(ChatRole.User, SystemPromptWithFunctions.Replace("[FUNCTIONS]", functionDefinitions.ToString())));
				}
			}

			// Call the wllama completion function
			_ = Task.Run(async () =>
			{
				await (await GetModule()).InvokeVoidAsync(
					"completeStreamWllama",
					interopInstance.ConvertMessage(convertedMessages),
					"wllama-model", // Model ID is not really used in wllama
					ChatMode == ChatMode.ChatWithThinking,
					options?.Tools?.Any() == true ? JsonSerializer.Serialize(CreateToolsArray(options.Tools)) : "[]"
				);
			});

			string totalText = string.Empty;

			while (true)
			{
				if (interopInstance.WllamaCompletions.TryDequeue(out WllamaCompletion? response))
				{
					if (response.IsStreamComplete)
					{
						if ((options?.Tools?.Any() ?? false) && !string.IsNullOrWhiteSpace(totalText))
						{
							if (TryParseToolCalls(totalText.Trim(), out var toolCalls))
							{
								foreach (var toolCall in toolCalls)
								{
									var output = await CallToolAsync(options, toolCall.Name, toolCall.Arguments);
									yield return new ChatResponseUpdate(ChatRole.Assistant, output);
								}
								yield break;
							}
							else
							{
								string content1 = totalText.Trim();
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
						logger.LogDebug("ChatRole.Tool: {Message}", content);
						yield return new ChatResponseUpdate(ChatRole.Tool, content ?? string.Empty);
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

		private object[] CreateToolsArray(IEnumerable<AITool> tools)
		{
			var toolsArray = new List<object>();
			
			foreach (var tool in tools.OfType<KernelFunction>())
			{
				var parameters = new Dictionary<string, object>
				{
					["type"] = "object",
					["properties"] = new Dictionary<string, object>(),
					["required"] = new List<string>()
				};

				var properties = (Dictionary<string, object>)parameters["properties"];
				var required = (List<string>)parameters["required"];

				foreach (var param in tool.Metadata.Parameters)
				{
					properties[param.Name] = new Dictionary<string, object>
					{
						["type"] = ConvertTypeToJsonSchema(param.ParameterType),
						["description"] = param.Description ?? param.Name
					};

					if (param.IsRequired)
					{
						required.Add(param.Name);
					}
				}

				toolsArray.Add(new
				{
					type = "function",
					function = new
					{
						name = tool.Name,
						description = tool.Description ?? tool.Name,
						parameters = parameters
					}
				});
			}

			return toolsArray.ToArray();
		}

		private string ConvertTypeToJsonSchema(Type? type)
		{
			if (type == null) return "string";
			
			return Type.GetTypeCode(type) switch
			{
				TypeCode.Boolean => "boolean",
				TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => "integer",
				TypeCode.Single or TypeCode.Double or TypeCode.Decimal => "number",
				_ => "string"
			};
		}

		private bool TryParseToolCalls(string content, out List<Microsoft.Extensions.AI.FunctionCallContent> toolCalls)
		{
			toolCalls = [];
			
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

						Dictionary<string, object?>? argumentsDict = null;
						if (function.TryGetProperty("arguments", out var argumentsProperty))
						{
							if (argumentsProperty.ValueKind == JsonValueKind.String)
							{
								var argumentsStr = argumentsProperty.GetString();
								if (!string.IsNullOrEmpty(argumentsStr))
								{
									try
									{
										using var argDoc = JsonDocument.Parse(argumentsStr);
										argumentsDict = new Dictionary<string, object?>();
										foreach (var prop in argDoc.RootElement.EnumerateObject())
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
									catch (JsonException ex)
									{
										logger.LogWarning(ex, "Failed to parse function arguments as JSON string: {Arguments}", argumentsStr);
										argumentsDict = new Dictionary<string, object?> { { "value", argumentsStr } };
									}
								}
							}
							else if (argumentsProperty.ValueKind == JsonValueKind.Object)
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
						}

						toolCalls.Add(new Microsoft.Extensions.AI.FunctionCallContent(
							id,
							name,
							argumentsDict ?? new Dictionary<string, object?>()
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
			return new ChatMessage(x.Role, x.Text?.Replace("<|thinking|>", "").Replace("<|/thinking|>", "").Trim() ?? string.Empty) { AuthorName = x.AuthorName };
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

		public TService? GetService<TService>(object? key = null) where TService : class => this as TService;

		public void Dispose() { }

		public async Task InitializeAsync(IProgress<InitializeProgress> OnProgress)
		{
			interopInstance = new WllamaInteropInstance(OnProgress);
			await (await GetModule()).InvokeVoidAsync(
				"initializeWllama",
				modelUrl,
				DotNetObjectReference.Create(interopInstance));
		}

		private async Task<IJSObjectReference> GetModule()
		{
			if (interopInstance == null)
			{
				throw new NotSupportedException("Interop instance is not initialized.");
			}

			module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/dist/wllama.interop.js");
			if (module == null)
			{
				throw new NotSupportedException("Module is not initialized.");
			}

			return module;
		}

		public IWebChatClient Clone()
		{
			return new WllamaChatClient(jsRuntime, logger, modelUrl)
			{
				interopInstance = interopInstance,
				module = module,
				ChatMode = this.ChatMode,
			};
		}

		private async Task<string> CallToolAsync(ChatOptions options, string name, IDictionary<string, object?> arguments)
		{
			var tool = options.Tools?.OfType<KernelFunction>()
				.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

			if (tool == null)
			{
				logger.LogWarning("Tool with name '{ToolName}' not found.", name);
				return $"Tool '{name}' not found.";
			}

			var kernelArgs = new KernelArguments();
			if (arguments != null)
			{
				foreach (var arg in arguments)
				{
					kernelArgs[arg.Key] = arg.Value;
				}
			}

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
	}
}