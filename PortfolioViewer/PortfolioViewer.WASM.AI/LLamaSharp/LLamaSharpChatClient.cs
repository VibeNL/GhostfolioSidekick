using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using LLama;
using LLama.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.LLamaSharp
{
	public class LLamaSharpChatClient : IWebChatClient
	{
		private readonly ILogger<LLamaSharpChatClient> logger;
		private readonly Dictionary<ChatMode, string> modelPaths;
		private readonly ModelDownloadService? downloadService;
		private readonly IJSRuntime? jsRuntime;
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
			this.jsRuntime = downloadService?.jsRuntime;
		}

		public async Task InitializeAsync(IProgress<InitializeProgress> OnProgress)
		{
			try
			{
				OnProgress.Report(new InitializeProgress(0.1, "Initializing LLamaSharp for WebAssembly..."));

				// Try to find or download a model file
				var modelPath = await FindOrDownloadModelAsync(OnProgress);
				if (string.IsNullOrEmpty(modelPath))
				{
					logger.LogWarning("No LLamaSharp model files found and download failed. Expected paths: {ModelPaths}", 
						string.Join(", ", modelPaths.Values));
					OnProgress.Report(new InitializeProgress(0.0, 
						"LLamaSharp not available - no suitable model found."));
					return;
				}

				OnProgress.Report(new InitializeProgress(0.7, $"Loading model from {Path.GetFileName(modelPath)}..."));

				// In WASM environment, use JavaScript-based initialization
				if (IsWasmEnvironment())
				{
					await InitializeWasmAsync(modelPath, OnProgress);
				}
				else
				{
					await InitializeNativeAsync(modelPath, OnProgress);
				}

				IsInitialized = true;
				OnProgress.Report(new InitializeProgress(1.0, "LLamaSharp initialized successfully"));
				
				logger.LogInformation("LLamaSharp initialized successfully with model: {ModelPath}", modelPath);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to initialize LLamaSharp");
				OnProgress.Report(new InitializeProgress(0.0, $"LLamaSharp initialization failed: {ex.Message}"));
				IsInitialized = false;
			}
		}

		/// <summary>
		/// Initialize LLamaSharp in WebAssembly environment using JavaScript backend
		/// </summary>
		private async Task InitializeWasmAsync(string modelPath, IProgress<InitializeProgress> OnProgress)
		{
			if (jsRuntime is null)
			{
				throw new InvalidOperationException("JSRuntime is required for WASM initialization");
			}

			OnProgress.Report(new InitializeProgress(0.8, "Initializing WASM LLama context..."));

			// Use JavaScript to initialize the model in WASM environment
			var success = await jsRuntime.InvokeAsync<bool>("llamaSharpWasm.initializeModel", modelPath, new
			{
				contextSize = 2048, // Smaller context for WASM
				threads = 1, // Single thread for WASM
				useGpu = false // CPU only in WASM for now
			});

			if (!success)
			{
				throw new InvalidOperationException("Failed to initialize LLamaSharp model in WASM environment");
			}

			OnProgress.Report(new InitializeProgress(0.95, "WASM LLama context ready"));
		}

		/// <summary>
		/// Initialize LLamaSharp using native libraries (server environment)
		/// </summary>
		private async Task InitializeNativeAsync(string modelPath, IProgress<InitializeProgress> OnProgress)
		{
			// Configure model parameters for server environment
			var parameters = new ModelParams(modelPath)
			{
				ContextSize = 2048,
				GpuLayerCount = 0, // CPU usage
				UseMemorymap = true, // Use memory mapping for better performance on server
				UseMemoryLock = false,
				Threads = Environment.ProcessorCount > 4 ? 4 : Environment.ProcessorCount
			};

			OnProgress.Report(new InitializeProgress(0.8, "Creating LLama weights..."));
			weights = LLamaWeights.LoadFromFile(parameters);
			
			OnProgress.Report(new InitializeProgress(0.9, "Creating LLama context..."));
			context = weights.CreateContext(parameters);
			
			OnProgress.Report(new InitializeProgress(0.95, "Creating chat session..."));
			var executor = new InteractiveExecutor(context);
			session = new ChatSession(executor);

			await Task.CompletedTask; // Make it async for consistency
		}

		/// <summary>
		/// Detects if running in WebAssembly environment
		/// </summary>
		private static bool IsWasmEnvironment()
		{
			// Check for WASM-specific indicators
			return System.Runtime.InteropServices.RuntimeInformation.OSDescription.Contains("Browser") ||
				   Environment.OSVersion.Platform == PlatformID.Other ||
				   Type.GetType("System.Runtime.InteropServices.JavaScript.JSHost") != null;
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
					progress.Report(new InitializeProgress(0.2, "No model found, attempting download..."));
					
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
				catch (NotSupportedException ex)
				{
					// Handle WASM environment gracefully
					logger.LogWarning(ex, "Model download not supported in current environment");
					progress.Report(new InitializeProgress(0.0, 
						"LLamaSharp not available in browser. WebLLM will be used instead for browser-based AI."));
				}
				catch (HttpRequestException ex) when (ex.Message.Contains("Failed to fetch"))
				{
					// Handle CORS/network issues
					logger.LogWarning(ex, "Network issues prevented model download");
					progress.Report(new InitializeProgress(0.0, 
						"Unable to download model due to network restrictions. WebLLM will be used instead."));
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
			if (!IsInitialized)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, 
					"LLamaSharp is not initialized. Please check that model files are available.");
				yield break;
			}

			// Convert messages to a single prompt
			var prompt = ConvertMessagesToPrompt(messages, options);
			
			logger.LogDebug("LLamaSharp prompt: {Prompt}", prompt);

			// Use different execution paths for WASM vs Native
			if (IsWasmEnvironment())
			{
				await foreach (var update in GetStreamingResponseWasmAsync(prompt, options, cancellationToken))
				{
					yield return update;
				}
			}
			else
			{
				await foreach (var update in GetStreamingResponseNativeAsync(prompt, options, cancellationToken))
				{
					yield return update;
				}
			}
		}

		/// <summary>
		/// Get streaming response using WASM JavaScript backend
		/// </summary>
		private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseWasmAsync(
			string prompt, 
			ChatOptions? options, 
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (jsRuntime is null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, "JSRuntime not available for WASM execution");
				yield break;
			}

			var responseBuilder = new StringBuilder();
			string? streamId = null;
			WasmStreamingResult? result = null;
			string? errorMessage = null;

			// Start the streaming generation in JavaScript
			try
			{
				streamId = await jsRuntime.InvokeAsync<string>("llamaSharpWasm.startStreaming", prompt, new
				{
					maxTokens = 1024,
					temperature = 0.7f,
					stopTokens = new[] { "</s>", "[INST]", "[/INST]" }
				});
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error starting WASM streaming");
				errorMessage = $"Error: {ex.Message}";
			}

			if (errorMessage is not null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, errorMessage);
				yield break;
			}

			if (string.IsNullOrEmpty(streamId))
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, "Failed to start streaming session");
				yield break;
			}

			// Poll for streaming results
			while (!cancellationToken.IsCancellationRequested)
			{
				errorMessage = null;
				try
				{
					result = await jsRuntime.InvokeAsync<WasmStreamingResult>("llamaSharpWasm.getStreamingResult", streamId);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error getting WASM streaming result");
					errorMessage = $"Error: {ex.Message}";
				}

				if (errorMessage is not null)
				{
					yield return new ChatResponseUpdate(ChatRole.Assistant, errorMessage);
					yield break;
				}
				
				if (result.IsComplete)
				{
					if (!string.IsNullOrEmpty(result.Text))
					{
						responseBuilder.Append(result.Text);
						yield return new ChatResponseUpdate(ChatRole.Assistant, result.Text);
					}
					break;
				}
				
				if (!string.IsNullOrEmpty(result.Text))
				{
					responseBuilder.Append(result.Text);
					
					// Check if we have a complete function call response
					if (options?.Tools?.Any() == true && TryParseToolCalls(responseBuilder.ToString(), out var toolCalls))
					{
						// Execute function calls
						foreach (var toolCall in toolCalls)
						{
							string toolResult;
							try
							{
								toolResult = await CallToolAsync(options, toolCall.Name, toolCall.Arguments ?? new Dictionary<string, object?>());
							}
							catch (Exception ex)
							{
								toolResult = $"Error executing tool: {ex.Message}";
							}
							yield return new ChatResponseUpdate(ChatRole.Assistant, toolResult);
						}
						yield break;
					}
					else
					{
						yield return new ChatResponseUpdate(ChatRole.Assistant, result.Text);
					}
				}
				
				if (result.HasError)
				{
					yield return new ChatResponseUpdate(ChatRole.Assistant, $"Error: {result.ErrorMessage}");
					yield break;
				}

				// Small delay to prevent overwhelming the browser
				await Task.Yield();
			}
		}

		/// <summary>
		/// Get streaming response using native LLamaSharp libraries
		/// </summary>
		private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseNativeAsync(
			string prompt, 
			ChatOptions? options, 
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			if (session == null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, 
					"LLamaSharp session is not initialized.");
				yield break;
			}

			var responseBuilder = new StringBuilder();

			// Create the inference parameters
			var inferenceParams = new InferenceParams
			{
				MaxTokens = 1024
			};

			// Generate response using ChatSession
			IAsyncEnumerable<string>? responseStream = null;
			string? errorMessage = null;

			try
			{
				responseStream = session.ChatAsync(new ChatHistory.Message(AuthorRole.User, prompt), inferenceParams);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during LLamaSharp streaming response");
				errorMessage = $"Error: {ex.Message}";
			}

			if (responseStream == null || errorMessage != null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, errorMessage ?? "Unknown error occurred");
				yield break;
			}

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

		/// <summary>
		/// Result structure for WASM streaming operations
		/// </summary>
		private class WasmStreamingResult
		{
			public string Text { get; set; } = string.Empty;
			public bool IsComplete { get; set; }
			public bool HasError { get; set; }
			public string ErrorMessage { get; set; } = string.Empty;
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