using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using LLama;
using LLama.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.LLamaSharp
{
	public class LLamaSharpChatClient : IWebChatClient
	{
		private readonly ILogger<LLamaSharpChatClient> logger;
		private readonly Dictionary<ChatMode, string> modelPaths;
		private readonly ModelDownloadService? downloadService;
		private LLamaWeights? weights;
		private LLamaContext? context;
		private ChatSession? session;
		
		public ChatMode ChatMode { get; set; } = ChatMode.Chat;
		public bool IsInitialized { get; private set; } = false;

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

		public LLamaSharpChatClient(
			ILogger<LLamaSharpChatClient> logger, 
			Dictionary<ChatMode, string> modelPaths, 
			ModelDownloadService? downloadService = null)
		{
			this.logger = logger;
			this.modelPaths = modelPaths;
			this.downloadService = downloadService;
		}

		public async Task InitializeAsync(IProgress<InitializeProgress> OnProgress)
		{
			try
			{
				OnProgress.Report(new InitializeProgress(0.1, "Initializing LLamaSharp CPU fallback..."));

				// Try to find or download a model file
				var modelPath = await FindOrDownloadModelAsync(OnProgress);
				if (string.IsNullOrEmpty(modelPath))
				{
					logger.LogWarning("No LLamaSharp model files found and download failed. Expected paths: {ModelPaths}", 
						string.Join(", ", modelPaths.Values));
					OnProgress.Report(new InitializeProgress(0.0, 
						"Error: No LLamaSharp model files found and download failed. Please check network connection."));
					return;
				}

				OnProgress.Report(new InitializeProgress(0.7, $"Loading model from {Path.GetFileName(modelPath)}..."));

				// Configure model parameters for CPU usage
				var parameters = new ModelParams(modelPath)
				{
					ContextSize = 2048, // Smaller context for WASM
					GpuLayerCount = 0, // Force CPU usage
					UseMemorymap = false, // Don't use memory mapping in WASM
					UseMemoryLock = false, // Don't lock memory in WASM
					Threads = 1 // Single thread for WASM
				};

				OnProgress.Report(new InitializeProgress(0.8, "Creating LLama weights..."));

				// Initialize the model
				weights = LLamaWeights.LoadFromFile(parameters);
				
				OnProgress.Report(new InitializeProgress(0.9, "Creating LLama context..."));
				
				context = weights.CreateContext(parameters);
				
				OnProgress.Report(new InitializeProgress(0.95, "Creating chat session..."));
				
				// Create a chat session
				var executor = new InteractiveExecutor(context);
				session = new ChatSession(executor);

				IsInitialized = true;
				OnProgress.Report(new InitializeProgress(1.0, "LLamaSharp CPU fallback initialized successfully"));
				
				logger.LogInformation("LLamaSharp initialized successfully with model: {ModelPath}", modelPath);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to initialize LLamaSharp");
				OnProgress.Report(new InitializeProgress(0.0, $"Error initializing LLamaSharp: {ex.Message}"));
				IsInitialized = false;
			}
		}

		private async Task<string?> FindOrDownloadModelAsync(IProgress<InitializeProgress> progress)
		{
			// First, try to find existing model files
			var existingModel = FindAvailableModel();
			if (!string.IsNullOrEmpty(existingModel))
			{
				logger.LogInformation("Found existing model at {ModelPath}", existingModel);
				return existingModel;
			}

			// If no existing model and we have a download service, try to download
			if (downloadService != null)
			{
				try
				{
					logger.LogInformation("No existing model found, attempting to download Phi-3 Mini...");
					progress.Report(new InitializeProgress(0.2, "No model found, downloading Phi-3 Mini..."));
					
					var downloadProgress = new Progress<InitializeProgress>(p =>
					{
						// Scale download progress to 20%-60% of total initialization
						var scaledProgress = 0.2 + (p.Progress * 0.4);
						progress.Report(new InitializeProgress(scaledProgress, p.Message));
					});

					var downloadedPath = await downloadService.EnsureModelDownloadedAsync("wwwroot/models", downloadProgress);
					
					// Update model paths to point to the downloaded model
					foreach (var key in modelPaths.Keys.ToList())
					{
						modelPaths[key] = downloadedPath;
					}
					
					return downloadedPath;
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to download model");
					progress.Report(new InitializeProgress(0.0, $"Model download failed: {ex.Message}"));
				}
			}

			return null;
		}

		private string? FindAvailableModel()
		{
			// Check if any of the configured model paths exist
			foreach (var modelPath in modelPaths.Values.Distinct())
			{
				if (ModelDownloadService.IsModelAvailable(modelPath))
				{
					return modelPath;
				}

				// Also check in common locations
				var commonPaths = new[]
				{
					Path.Combine("models", Path.GetFileName(modelPath)),
					Path.Combine("wwwroot", "models", Path.GetFileName(modelPath)),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "llama", Path.GetFileName(modelPath))
				};

				foreach (var commonPath in commonPaths)
				{
					if (ModelDownloadService.IsModelAvailable(commonPath))
					{
						return commonPath;
					}
				}
			}

			return null;
		}

		public async Task<ChatResponse> GetResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			if (!IsInitialized || session == null)
			{
				return new ChatResponse(new ChatMessage(ChatRole.Assistant, 
					"LLamaSharp CPU fallback is not initialized. Please check that model files are available."));
			}

			var responseBuilder = new StringBuilder();
			await foreach (var chunk in GetStreamingResponseAsync(messages, options, cancellationToken))
			{
				responseBuilder.Append(chunk.Text);
			}

			return new ChatResponse(new ChatMessage(ChatRole.Assistant, responseBuilder.ToString()));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (!IsInitialized || session == null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, 
					"LLamaSharp CPU fallback is not initialized. Please check that model files are available.");
				yield break;
			}

			// Convert messages to a single prompt
			var prompt = ConvertMessagesToPrompt(messages, options);
			
			logger.LogDebug("LLamaSharp prompt: {Prompt}", prompt);

			var responseBuilder = new StringBuilder();

			// Create the inference parameters
			var inferenceParams = new InferenceParams
			{
				MaxTokens = 1024
			};

			// Generate response using ChatSession - handle errors separately
			IAsyncEnumerable<string>? responseStream = null;
			string? errorMessage = null;

			// Try to create the response stream
			try
			{
				responseStream = session.ChatAsync(new ChatHistory.Message(AuthorRole.User, prompt), inferenceParams);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during LLamaSharp streaming response");
				errorMessage = $"Error: {ex.Message}";
			}

			// If there was an error creating the stream, yield the error and exit
			if (responseStream == null || errorMessage != null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, errorMessage ?? "Unknown error occurred");
				yield break;
			}

			// Process the response stream - no try-catch around yield statements
			await foreach (var text in responseStream)
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				responseBuilder.Append(text);
				
				// Check if we have a complete function call response
				if (options?.Tools?.Any() == true && TryParseToolCalls(responseBuilder.ToString(), out var toolCalls))
				{
					// Execute function calls
					foreach (var toolCall in toolCalls)
					{
						var result = await CallToolAsync(options, toolCall.Name, toolCall.Arguments ?? new Dictionary<string, object?>());
						yield return new ChatResponseUpdate(ChatRole.Assistant, result);
					}
					yield break;
				}
				else
				{
					yield return new ChatResponseUpdate(ChatRole.Assistant, text);
				}
			}
		}

		private string ConvertMessagesToPrompt(IEnumerable<ChatMessage> messages, ChatOptions? options)
		{
			var promptBuilder = new StringBuilder();

			// Add function calling instructions if needed
			if (ChatMode == ChatMode.FunctionCalling && options?.Tools?.Any() == true)
			{
				var functions = options.Tools.OfType<KernelFunction>().ToList();
				if (functions.Any())
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

					promptBuilder.AppendLine(SystemPromptWithFunctions.Replace("[FUNCTIONS]", functionDefinitions.ToString()));
					promptBuilder.AppendLine();
				}
			}

			// Convert messages to a simple prompt format
			foreach (var message in messages.Where(m => !string.IsNullOrWhiteSpace(m.Text)))
			{
				var role = GetRoleString(message.Role);
				promptBuilder.AppendLine($"{role}: {message.Text}");
			}

			promptBuilder.AppendLine("Assistant:");
			return promptBuilder.ToString();
		}

		private string GetRoleString(ChatRole role)
		{
			if (role == ChatRole.System) return "System";
			if (role == ChatRole.User) return "User";
			if (role == ChatRole.Assistant) return "Assistant";
			return "User";
		}

		private bool TryParseToolCalls(string content, out List<Microsoft.Extensions.AI.FunctionCallContent> toolCalls)
		{
			toolCalls = new List<Microsoft.Extensions.AI.FunctionCallContent>();
			
			if (string.IsNullOrWhiteSpace(content))
				return false;

			try
			{
				// Look for JSON structure in the response
				var jsonStart = content.IndexOf('{');
				var jsonEnd = content.LastIndexOf('}');
				
				if (jsonStart == -1 || jsonEnd == -1 || jsonEnd <= jsonStart)
					return false;

				var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
				
				using var doc = JsonDocument.Parse(jsonContent);
				if (doc.RootElement.TryGetProperty("tool_calls", out var toolCallsArray) && 
				    toolCallsArray.ValueKind == JsonValueKind.Array)
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
									catch (JsonException)
									{
										argumentsDict = new Dictionary<string, object?> { { "value", argumentsStr } };
									}
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
				logger.LogWarning(ex, "Failed to parse tool calls from LLamaSharp response: {Content}", content);
			}

			return false;
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

			try
			{
				var kernelArgs = new KernelArguments();
				if (arguments != null)
				{
					foreach (var arg in arguments)
					{
						kernelArgs[arg.Key] = arg.Value;
					}
				}

				var result = await tool.InvokeAsync(kernelArgs);
				var output = result?.ToString() ?? "[Function returned null]";
				logger.LogInformation("LLamaSharp tool '{ToolName}' executed with output: {Output}", name, output);
				return output;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error executing LLamaSharp tool '{ToolName}' with arguments: {Arguments}", 
					name, JsonSerializer.Serialize(arguments));
				return $"Error executing tool '{name}': {ex.Message}";
			}
		}

		public IWebChatClient Clone()
		{
			var clone = new LLamaSharpChatClient(logger, modelPaths, downloadService)
			{
				ChatMode = this.ChatMode,
				weights = this.weights,
				context = this.context,
				session = this.session,
				IsInitialized = this.IsInitialized
			};
			return clone;
		}

		public object? GetService(Type serviceType, object? serviceKey) => this;

		public TService? GetService<TService>(object? key = null) where TService : class => this as TService;

		public void Dispose()
		{
			// ChatSession doesn't implement IDisposable in recent versions
			session = null;
			context?.Dispose();
			weights?.Dispose();
		}
	}
}