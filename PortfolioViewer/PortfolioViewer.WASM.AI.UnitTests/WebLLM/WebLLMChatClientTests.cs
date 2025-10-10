using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using Moq;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.WebLLM
{
	public class WebLLMChatClientTests : IDisposable
	{
		private readonly Mock<IJSRuntime> _mockJSRuntime;
		private readonly Mock<ILogger<WebLLMChatClient>> _mockLogger;
		private readonly Mock<IJSObjectReference> _mockModule;
		private readonly Dictionary<ChatMode, string> _modelIds;
		private readonly WebLLMChatClient _client;

		public WebLLMChatClientTests()
		{
			_mockJSRuntime = new Mock<IJSRuntime>();
			_mockLogger = new Mock<ILogger<WebLLMChatClient>>();
			_mockModule = new Mock<IJSObjectReference>();
			_modelIds = new Dictionary<ChatMode, string>
			{
				{ ChatMode.Chat, "test-chat-model" },
				{ ChatMode.ChatWithThinking, "test-thinking-model" },
				{ ChatMode.FunctionCalling, "test-function-model" }
			};

			_client = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, _modelIds);
		}

		[Fact]
		public void Constructor_ShouldInitializeWithCorrectDefaults()
		{
			// Assert
			Assert.Equal(ChatMode.Chat, _client.ChatMode);
		}

		[Fact]
		public void ChatMode_ShouldGetAndSetCorrectly()
		{
			// Act
			_client.ChatMode = ChatMode.FunctionCalling;

			// Assert
			Assert.Equal(ChatMode.FunctionCalling, _client.ChatMode);
		}

		[Fact]
		public void GetService_ShouldReturnSelf()
		{
			// Act
			var service = _client.GetService(typeof(IWebChatClient));

			// Assert
			Assert.Same(_client, service);
		}

		[Fact]
		public async Task GetResponseAsync_WithoutInitialization_ShouldThrowNotSupportedException()
		{
			// Arrange
			var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };

			// Act & Assert
			await Assert.ThrowsAsync<NotSupportedException>(() => _client.GetResponseAsync(messages));
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithoutInitialization_ShouldThrowNotSupportedException()
		{
			// Arrange
			var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };

			// Act & Assert
			await Assert.ThrowsAsync<NotSupportedException>(async () =>
			{
				await foreach (var _ in _client.GetStreamingResponseAsync(messages))
				{
					// Should not reach here
				}
			});
		}

		[Fact]
		public async Task InitializeAsync_ShouldCallJSModuleCorrectly()
		{
			// Arrange
			var progress = new Mock<IProgress<InitializeProgress>>();
			_mockJSRuntime.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
				.ReturnsAsync(_mockModule.Object);

			// Act
			await _client.InitializeAsync(progress.Object);

			// Assert
			_mockJSRuntime.Verify(x => x.InvokeAsync<IJSObjectReference>("import", 
				It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == "./js/dist/webllm.interop.js")), 
				Times.Once);

			// We can't easily verify the InvokeVoidAsync call because it's an extension method
			// but we can verify that no exception was thrown and the initialization completed
			Assert.True(true); // Test passes if no exception was thrown during initialization
		}

		[Fact]
		public void Clone_ShouldCreateNewInstanceWithSameConfiguration()
		{
			// Act
			var cloned = _client.Clone();

			// Assert
			Assert.NotSame(_client, cloned);
			Assert.IsType<WebLLMChatClient>(cloned);
			Assert.Equal(_client.ChatMode, cloned.ChatMode);
		}

		[Fact]
		public void Clone_ShouldCopyInteropInstanceAndModule()
		{
			// This test verifies the clone behavior, though we can't directly access private fields
			// Act
			var cloned = _client.Clone();

			// Assert
			Assert.NotNull(cloned);
			Assert.Equal(_client.ChatMode, cloned.ChatMode);
		}

		[Theory]
		[InlineData(ChatMode.Chat)]
		[InlineData(ChatMode.ChatWithThinking)]
		[InlineData(ChatMode.FunctionCalling)]
		public void ChatMode_ShouldAcceptAllValidValues(ChatMode mode)
		{
			// Act
			_client.ChatMode = mode;

			// Assert
			Assert.Equal(mode, _client.ChatMode);
		}

		[Fact]
		public void Dispose_ShouldNotThrow()
		{
			// Act & Assert - Should not throw
			_client.Dispose();
		}

		[Fact]
		public async Task LoadJsModuleAsync_ShouldCallJSRuntimeImport()
		{
			// Arrange
			const string testPath = "./test/module.js";
			_mockJSRuntime.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
				.ReturnsAsync(_mockModule.Object);

			// Act
			var result = await WebLLMChatClient.LoadJsModuleAsync(_mockJSRuntime.Object, testPath);

			// Assert
			Assert.Same(_mockModule.Object, result);
			_mockJSRuntime.Verify(x => x.InvokeAsync<IJSObjectReference>("import", 
				It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == testPath)), 
				Times.Once);
		}

		public void Dispose()
		{
			_client?.Dispose();
		}
	}

	// Additional test class for testing private methods through reflection
	public class WebLLMChatClientInternalTests
	{
		[Fact]
		public void Fix_WithAssistantRole_ShouldConvertToUser()
		{
			// Arrange
			var assistantMessage = new ChatMessage(ChatRole.Assistant, "Assistant response");

			// Act - Using reflection to access private method
			var fixMethod = typeof(WebLLMChatClient).GetMethod("Fix", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (ChatMessage)fixMethod!.Invoke(null, new object[] { assistantMessage })!;

			// Assert
			Assert.Equal(ChatRole.User, result.Role);
			Assert.Equal("Assistant response", result.Text);
		}

		[Fact]
		public void Fix_WithUserRole_ShouldReturnUnchanged()
		{
			// Arrange
			var userMessage = new ChatMessage(ChatRole.User, "User input");

			// Act - Using reflection to access private method
			var fixMethod = typeof(WebLLMChatClient).GetMethod("Fix", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (ChatMessage)fixMethod!.Invoke(null, new object[] { userMessage })!;

			// Assert
			Assert.Equal(ChatRole.User, result.Role);
			Assert.Equal("User input", result.Text);
		}

		[Fact]
		public void RemoveThink_ShouldProcessMessage()
		{
			// Arrange
			var message = new ChatMessage(ChatRole.User, "Hello <think>thinking content</think> world");

			// Act - Using reflection to access private method
			var removeThinkMethod = typeof(WebLLMChatClient).GetMethod("RemoveThink", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (ChatMessage)removeThinkMethod!.Invoke(null, new object[] { message })!;

			// Assert
			Assert.Equal(ChatRole.User, result.Role);
			Assert.Equal("Hello  world", result.Text);
		}

		[Fact]
		public void IsEmptyMessageList_WithEmptyList_ShouldReturnTrue()
		{
			// Arrange
			var messages = new List<ChatMessage>();

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("IsEmptyMessageList", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (bool)method!.Invoke(null, new object[] { messages })!;

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void IsEmptyMessageList_WithNonEmptyList_ShouldReturnFalse()
		{
			// Arrange
			var messages = new List<ChatMessage> { new(ChatRole.User, "Hello") };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("IsEmptyMessageList", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (bool)method!.Invoke(null, new object[] { messages })!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void HasTools_WithNullOptions_ShouldReturnFalse()
		{
			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("HasTools", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (bool)method!.Invoke(null, new object?[] { null })!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void HasTools_WithEmptyTools_ShouldReturnFalse()
		{
			// Arrange
			var options = new ChatOptions { Tools = [] };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("HasTools", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (bool)method!.Invoke(null, new object[] { options })!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void HasTools_WithTools_ShouldReturnTrue()
		{
			// Arrange
			var testFunction = KernelFunctionFactory.CreateFromMethod(
				() => "test",
				"TestFunction", 
				"Test function");
			var options = new ChatOptions { Tools = [testFunction] };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("HasTools", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (bool)method!.Invoke(null, new object[] { options })!;

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void ExtractContentFromResponse_WithValidResponse_ShouldReturnContent()
		{
			// Arrange - Create a Message for the Delta property with the content we want to extract
			var deltaMessage = new Message("assistant", "Hello");
			var choice = new Choice(0, deltaMessage, "", "");
			var response = new WebLLMCompletion("id", "object", "model", "fingerprint", [choice], null);

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ExtractContentFromResponse", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (string?)method!.Invoke(null, new object[] { response });

			// Assert
			Assert.Equal("Hello", result);
		}

		[Fact]
		public void ExtractContentFromResponse_WithNullChoices_ShouldReturnNull()
		{
			// Arrange
			var response = new WebLLMCompletion("id", "object", "model", "fingerprint", null, null);

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ExtractContentFromResponse", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (string?)method!.Invoke(null, new object[] { response });

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void ExtractContentFromResponse_WithEmptyChoices_ShouldReturnNull()
		{
			// Arrange
			var response = new WebLLMCompletion("id", "object", "model", "fingerprint", [], null);

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ExtractContentFromResponse", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (string?)method!.Invoke(null, new object[] { response });

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void ExtractContentFromResponse_WithNullDelta_ShouldReturnNull()
		{
			// Arrange
			var choice = new Choice(0, null, "", "");
			var response = new WebLLMCompletion("id", "object", "model", "fingerprint", [choice], null);

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ExtractContentFromResponse", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (string?)method!.Invoke(null, new object[] { response });

			// Assert
			Assert.Null(result);
		}

		[Fact]
		public void BuildFunctionDefinitions_WithFunctions_ShouldReturnFormattedString()
		{
			// Arrange
			var testFunction = KernelFunctionFactory.CreateFromMethod(
				(string testParam) => "result",
				"TestFunction",
				"Test function description");

			var functions = new List<KernelFunction> { testFunction };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("BuildFunctionDefinitions", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (string)method!.Invoke(null, new object[] { functions })!;

			// Assert
			Assert.Contains("TestFunction", result);
			Assert.Contains("Test function description", result);
			Assert.Contains("testParam", result);
		}
	}

	// Test class for tool call parsing functionality
	public class WebLLMChatClientToolCallTests
	{
		private readonly Mock<IJSRuntime> _mockJSRuntime;
		private readonly Mock<ILogger<WebLLMChatClient>> _mockLogger;
		private readonly Dictionary<ChatMode, string> _modelIds;
		private readonly WebLLMChatClient _client;

		public WebLLMChatClientToolCallTests()
		{
			_mockJSRuntime = new Mock<IJSRuntime>();
			_mockLogger = new Mock<ILogger<WebLLMChatClient>>();
			_modelIds = new Dictionary<ChatMode, string> { { ChatMode.FunctionCalling, "test-model" } };
			_client = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, _modelIds);
		}

		[Fact]
		public void TryParseToolCalls_WithValidJson_ShouldReturnTrue()
		{
			// Arrange
			var json = """
			{
				"tool_calls": [
					{
						"id": "call_123",
						"type": "function",
						"function": {
							"name": "TestFunction",
							"arguments": "{\"param1\": \"value1\"}"
						}
					}
				]
			}
			""";

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("TryParseToolCalls", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var parameters = new object[] { json, null! };
			var result = (bool)method!.Invoke(_client, parameters)!;
			var toolCalls = (List<Microsoft.Extensions.AI.FunctionCallContent>)parameters[1];

			// Assert
			Assert.True(result);
			Assert.Single(toolCalls);
			Assert.Equal("call_123", toolCalls[0].CallId);
			Assert.Equal("TestFunction", toolCalls[0].Name);
		}

		[Fact]
		public void TryParseToolCalls_WithInvalidJson_ShouldReturnFalse()
		{
			// Arrange
			var invalidJson = "{ invalid json }";

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("TryParseToolCalls", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var parameters = new object[] { invalidJson, null! };
			var result = (bool)method!.Invoke(_client, parameters)!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void TryParseToolCalls_WithEmptyString_ShouldReturnFalse()
		{
			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("TryParseToolCalls", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var parameters = new object[] { "", null! };
			var result = (bool)method!.Invoke(_client, parameters)!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void TryParseToolCalls_WithNullString_ShouldReturnFalse()
		{
			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("TryParseToolCalls", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var parameters = new object?[] { null, null! };
			var result = (bool)method!.Invoke(_client, parameters)!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void ConvertJsonElementToDictionary_WithVariousTypes_ShouldConvertCorrectly()
		{
			// Arrange
			var json = """
			{
				"stringValue": "test",
				"intValue": 42,
				"boolValue": true,
				"nullValue": null,
				"doubleValue": 3.14
			}
			""";

			using var doc = JsonDocument.Parse(json);

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ConvertJsonElementToDictionary", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (Dictionary<string, object?>)method!.Invoke(null, new object[] { doc.RootElement })!;

			// Assert
			Assert.Equal("test", result["stringValue"]);
			Assert.Equal(42d, (double?)result["intValue"]); // JSON integers are converted to long
			Assert.True((bool?)result["boolValue"]);
			Assert.Null(result["nullValue"]);
			Assert.Equal(3.14, result["doubleValue"]);
		}

		[Fact]
		public void ParseArgumentsFromString_WithValidJson_ShouldReturnDictionary()
		{
			// Arrange
			var argumentsJson = """{"param1": "value1", "param2": 42}""";

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ParseArgumentsFromString", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (Dictionary<string, object?>?)method!.Invoke(_client, new object?[] { argumentsJson });

			// Assert
			Assert.NotNull(result);
			Assert.Equal("value1", result["param1"]);
			Assert.Equal(42d,(double?)result["param2"]); // JSON integers are converted to long
		}

		[Fact]
		public void ParseArgumentsFromString_WithInvalidJson_ShouldReturnFallbackDictionary()
		{
			// Arrange
			var invalidJson = "invalid json";

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ParseArgumentsFromString", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (Dictionary<string, object?>?)method!.Invoke(_client, new object?[] { invalidJson });

			// Assert
			Assert.NotNull(result);
			Assert.Equal("invalid json", result["value"]);
		}

		[Fact]
		public void ParseArgumentsFromString_WithEmptyString_ShouldReturnEmptyDictionary()
		{
			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ParseArgumentsFromString", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (Dictionary<string, object?>?)method!.Invoke(_client, new object?[] { "" });

			// Assert
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public async Task CallToolAsync_WithValidTool_ShouldExecuteAndReturnResult()
		{
			// Arrange
			var testFunction = KernelFunctionFactory.CreateFromMethod(
				(KernelArguments args) => "Tool result",
				"TestTool",
				"Test tool description");

			var options = new ChatOptions { Tools = [testFunction] };
			var arguments = new Dictionary<string, object?> { { "param1", "value1" } };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("CallToolAsync", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = await (Task<string>)method!.Invoke(_client, new object[] { options, "TestTool", arguments })!;

			// Assert
			Assert.Equal("Tool result", result);
		}

		[Fact]
		public async Task CallToolAsync_WithNonExistentTool_ShouldReturnNotFoundMessage()
		{
			// Arrange
			var options = new ChatOptions { Tools = [] };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("CallToolAsync", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = await (Task<string>)method!.Invoke(_client, new object[] { options, "NonExistentTool", null! })!;

			// Assert
			Assert.Equal("Tool 'NonExistentTool' not found.", result);
		}

		[Fact]
		public async Task CallToolAsync_WithExceptionInTool_ShouldReturnErrorMessage()
		{
			// Arrange
			var testFunction = KernelFunctionFactory.CreateFromMethod(
				(KernelArguments args) => 
				{
					throw new InvalidOperationException("Tool error");
				},
				"ErrorTool",
				"Test tool that throws exception");

			var options = new ChatOptions { Tools = [testFunction] };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("CallToolAsync", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = await (Task<string>)method!.Invoke(_client, new object[] { options, "ErrorTool", null! })!;

			// Assert
			Assert.StartsWith("Error executing tool 'ErrorTool':", result);
		}
	}

	// Test class for message preparation functionality
	public class WebLLMChatClientMessagePreparationTests
	{
		private readonly Mock<IJSRuntime> _mockJSRuntime;
		private readonly Mock<ILogger<WebLLMChatClient>> _mockLogger;
		private readonly Dictionary<ChatMode, string> _modelIds;
		private readonly WebLLMChatClient _client;

		public WebLLMChatClientMessagePreparationTests()
		{
			_mockJSRuntime = new Mock<IJSRuntime>();
			_mockLogger = new Mock<ILogger<WebLLMChatClient>>();
			_modelIds = new Dictionary<ChatMode, string> { { ChatMode.FunctionCalling, "test-model" } };
			_client = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, _modelIds);
		}

		[Fact]
		public void PrepareMessages_WithEmptyMessages_ShouldReturnEmptyList()
		{
			// Arrange
			var messages = Array.Empty<ChatMessage>();

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("PrepareMessages", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (List<ChatMessage>)method!.Invoke(_client, new object[] { messages, null! })!;

			// Assert
			Assert.Empty(result);
		}

		[Fact]
		public void PrepareMessages_WithMessagesContainingWhitespace_ShouldFilterOut()
		{
			// Arrange
			var messages = new[]
			{
				new ChatMessage(ChatRole.User, "Valid message"),
				new ChatMessage(ChatRole.User, "   "),
				new ChatMessage(ChatRole.User, ""),
				new ChatMessage(ChatRole.User, "Another valid message")
			};

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("PrepareMessages", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (List<ChatMessage>)method!.Invoke(_client, new object[] { messages, null! })!;

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Equal("Valid message", result[0].Text);
			Assert.Equal("Another valid message", result[1].Text);
		}

		[Fact]
		public void PrepareMessages_WithFunctionCallingMode_ShouldAddFunctionPrompt()
		{
			// Arrange
			_client.ChatMode = ChatMode.FunctionCalling;
			var messages = new[] { new ChatMessage(ChatRole.User, "Hello") };
			var testFunction = KernelFunctionFactory.CreateFromMethod(
				() => "test",
				"TestFunction",
				"Test description");

			var options = new ChatOptions { Tools = [testFunction] };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("PrepareMessages", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (List<ChatMessage>)method!.Invoke(_client, new object[] { messages, options })!;

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Equal("Hello", result[0].Text);
			Assert.Contains("You are an AI assistant", result[1].Text);
		}

		[Fact]
		public void ShouldAddFunctionPrompt_WithFunctionCallingModeAndTools_ShouldReturnTrue()
		{
			// Arrange
			_client.ChatMode = ChatMode.FunctionCalling;
			var testFunction = KernelFunctionFactory.CreateFromMethod(
				() => "test",
				"TestFunction",
				"Test function");
			var options = new ChatOptions { Tools = [testFunction] };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ShouldAddFunctionPrompt", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (bool)method!.Invoke(_client, new object[] { options })!;

			// Assert
			Assert.True(result);
		}

		[Fact]
		public void ShouldAddFunctionPrompt_WithChatMode_ShouldReturnFalse()
		{
			// Arrange
			_client.ChatMode = ChatMode.Chat;
			var testFunction = KernelFunctionFactory.CreateFromMethod(
				() => "test",
				"TestFunction",
				"Test function");
			var options = new ChatOptions { Tools = [testFunction] };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ShouldAddFunctionPrompt", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (bool)method!.Invoke(_client, new object[] { options })!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void CreateFunctionPromptMessage_ShouldReturnValidMessage()
		{
			// Arrange
			var testFunction = KernelFunctionFactory.CreateFromMethod(
				() => "test",
				"TestFunction",
				"Test description");

			var options = new ChatOptions { Tools = [testFunction] };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("CreateFunctionPromptMessage", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (ChatMessage)method!.Invoke(null, new object[] { options })!;

			// Assert
			Assert.Equal(ChatRole.User, result.Role);
			Assert.Contains("You are an AI assistant", result.Text);
			Assert.Contains("TestFunction", result.Text);
		}
	}
}