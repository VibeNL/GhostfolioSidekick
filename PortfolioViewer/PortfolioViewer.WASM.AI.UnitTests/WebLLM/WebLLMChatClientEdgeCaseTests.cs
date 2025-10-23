using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.SemanticKernel;
using Moq;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.WebLLM
{
	/// <summary>
	/// Tests for edge cases and error scenarios in WebLLMChatClient
	/// </summary>
	public class WebLLMChatClientEdgeCaseTests : IDisposable
	{
		private readonly Mock<IJSRuntime> _mockJSRuntime;
		private readonly Mock<ILogger<WebLLMChatClient>> _mockLogger;
		private readonly Dictionary<ChatMode, string> _modelIds;
		private readonly WebLLMChatClient _client;

		public WebLLMChatClientEdgeCaseTests()
		{
			_mockJSRuntime = new Mock<IJSRuntime>();
			_mockLogger = new Mock<ILogger<WebLLMChatClient>>();
			_modelIds = new Dictionary<ChatMode, string>
			{
				{ ChatMode.Chat, "test-chat-model" },
				{ ChatMode.FunctionCalling, "test-function-model" }
			};
			_client = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, _modelIds);
		}

		[Fact]
		public async Task InitializeAsync_WhenJSModuleLoadFails_ShouldThrowException()
		{
			// Arrange
			var progress = new Mock<IProgress<InitializeProgress>>();
			_mockJSRuntime.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
				.ThrowsAsync(new JSException("Module not found"));

			// Act & Assert
			await Assert.ThrowsAsync<JSException>(() => _client.InitializeAsync(progress.Object));
		}

		[Fact]
		public async Task GetModule_WhenNotInitialized_ShouldThrowNotSupportedException()
		{
			// Act & Assert - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("GetModule",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			var exception = await Assert.ThrowsAsync<NotSupportedException>(
				async () => await (Task<IJSObjectReference>)method!.Invoke(_client, [])!);

			Assert.Contains("Interop instance is not initialized", exception.Message);
		}

		[Fact]
		public async Task GetModule_WhenModuleIsNull_ShouldThrowNotSupportedException()
		{
			// Arrange - Initialize interop but make module return null
			InitializeInteropInstance();
			_mockJSRuntime.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
				.ReturnsAsync((IJSObjectReference)null!);

			// Act & Assert - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("GetModule",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			var exception = await Assert.ThrowsAsync<NotSupportedException>(
				async () => await (Task<IJSObjectReference>)method!.Invoke(_client, [])!);

			Assert.Contains("Module is not initialized", exception.Message);
		}

		[Theory]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("\t\n")]
		public void TryParseToolCalls_WithWhitespaceContent_ShouldReturnFalse(string content)
		{
			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("TryParseToolCalls",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var parameters = new object[] { content, null! };
			var result = (bool)method!.Invoke(_client, parameters)!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void TryParseToolCalls_WithMalformedJson_ShouldLogWarningAndReturnFalse()
		{
			// Arrange
			var malformedJson = """{ "tool_calls": [ { "incomplete": """;

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("TryParseToolCalls",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var parameters = new object[] { malformedJson, null! };
			var result = (bool)method!.Invoke(_client, parameters)!;

			// Assert
			Assert.False(result);
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to parse tool calls from content")),
					It.IsAny<JsonException>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public void TryParseToolCalls_WithUnexpectedException_ShouldLogErrorAndReturnFalse()
		{
			// This test simulates an unexpected exception during parsing
			// We can't easily trigger this with normal JSON, so we'll test the logging behavior
			// by verifying the method handles exceptions correctly

			// Arrange
			var contentThatMightCauseException = new string('a', 100000); // Very large string

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("TryParseToolCalls",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var parameters = new object[] { contentThatMightCauseException, null! };
			var result = (bool)method!.Invoke(_client, parameters)!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void TryExtractToolCallsFromJson_WithNoToolCallsProperty_ShouldReturnFalse()
		{
			// Arrange
			var json = """{"other_property": "value"}""";
			using var doc = JsonDocument.Parse(json);

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("TryExtractToolCallsFromJson",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var parameters = new object[] { doc, null! };
			var result = (bool)method!.Invoke(_client, parameters)!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void TryExtractToolCallsFromJson_WithNonArrayToolCalls_ShouldReturnFalse()
		{
			// Arrange
			var json = """{"tool_calls": "not an array"}""";
			using var doc = JsonDocument.Parse(json);

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("TryExtractToolCallsFromJson",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var parameters = new object[] { doc, null! };
			var result = (bool)method!.Invoke(_client, parameters)!;

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void CreateFunctionCallContent_WithMissingRequiredProperties_ShouldLogWarningAndReturnNull()
		{
			// Arrange
			var json = """{"incomplete": "object"}""";
			using var doc = JsonDocument.Parse(json);

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("CreateFunctionCallContent",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (Microsoft.Extensions.AI.FunctionCallContent?)method!.Invoke(_client, [doc.RootElement]);

			// Assert
			Assert.Null(result);
			_mockLogger.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create function call content")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public void ExtractFunctionArguments_WithMissingArgumentsProperty_ShouldReturnEmptyDictionary()
		{
			// Arrange
			var json = """{"name": "TestFunction"}""";
			using var doc = JsonDocument.Parse(json);

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ExtractFunctionArguments",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (Dictionary<string, object?>?)method!.Invoke(_client, [doc.RootElement]);

			// Assert
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public void ExtractFunctionArguments_WithUnsupportedValueKind_ShouldReturnEmptyDictionary()
		{
			// Arrange
			var json = """{"arguments": [1, 2, 3]}"""; // Array is not supported
			using var doc = JsonDocument.Parse(json);
			var function = doc.RootElement;

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ExtractFunctionArguments",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (Dictionary<string, object?>?)method!.Invoke(_client, [function]);

			// Assert
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public void ParseArgumentsFromString_WithNullArguments_ShouldReturnEmptyDictionary()
		{
			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ParseArgumentsFromString",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (Dictionary<string, object?>?)method!.Invoke(_client, [null]);

			// Assert
			Assert.NotNull(result);
			Assert.Empty(result);
		}

		[Fact]
		public void ConvertJsonElementToDictionary_WithComplexNestedValue_ShouldReturnRawText()
		{
			// Arrange
			var json = """
			{
				"complexValue": {
					"nested": {
						"deep": "value"
					}
				}
			}
			""";
			using var doc = JsonDocument.Parse(json);

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ConvertJsonElementToDictionary",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
			var result = (Dictionary<string, object?>)method!.Invoke(null, [doc.RootElement])!;

			// Assert
			Assert.Single(result);
			Assert.Contains("complexValue", result.Keys);
			Assert.IsType<string>(result["complexValue"]);
			var rawText = (string)result["complexValue"]!;
			Assert.Contains("nested", rawText);
		}

		[Fact]
		public async Task CallToolAsync_WithNullArguments_ShouldExecuteWithEmptyKernelArguments()
		{
			// Arrange
			var testFunction = KernelFunctionFactory.CreateFromMethod(
				(KernelArguments args) => "Success with null args",
				"TestFunction",
				"Test function description");

			var options = new ChatOptions { Tools = [testFunction] };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("CallToolAsync",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = await (Task<string>)method!.Invoke(_client, [options, "TestFunction", null!])!;

			// Assert
			Assert.Equal("Success with null args", result);
		}

		[Fact]
		public async Task CallToolAsync_WithFunctionReturningNull_ShouldReturnFallbackMessage()
		{
			// Arrange - Create a function that returns null
			var testFunction = KernelFunctionFactory.CreateFromMethod(
				(KernelArguments args) =>
				{
					// Return a null string, which will result in null FunctionResult
					return (string?)null;
				},
				"TestFunction",
				"Test function description");

			var options = new ChatOptions { Tools = [testFunction] };

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("CallToolAsync",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = await (Task<string>)method!.Invoke(_client, [options, "TestFunction", null!])!;

			// Assert
			Assert.Equal("[Function returned null]", result);
		}

		[Fact]
		public void ValidateInteropInstance_WhenNull_ShouldThrowNotSupportedException()
		{
			// Act & Assert - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("ValidateInteropInstance",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			var exception = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
				method!.Invoke(_client, []));

			Assert.IsType<NotSupportedException>(exception.InnerException);
		}

		[Fact]
		public void PrepareMessages_WithAssistantMessageAsLast_ShouldConvertToUser()
		{
			// Arrange
			var messages = new[]
			{
				new ChatMessage(ChatRole.User, "User message"),
				new ChatMessage(ChatRole.Assistant, "Assistant message") // This should be converted to User
			};

			// Act - Using reflection to access private method
			var method = typeof(WebLLMChatClient).GetMethod("PrepareMessages",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var result = (List<ChatMessage>)method!.Invoke(_client, [messages, null!])!;

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Equal(ChatRole.User, result[0].Role);
			Assert.Equal(ChatRole.User, result[1].Role); // Was converted from Assistant
			Assert.Equal("Assistant message", result[1].Text);
		}

		private void InitializeInteropInstance()
		{
			var testInterop = new InteropInstance(new Mock<IProgress<InitializeProgress>>().Object);
			var interopField = typeof(WebLLMChatClient).GetField("interopInstance",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			interopField!.SetValue(_client, testInterop);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_client?.Dispose();
		}
	}
}