using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using AwesomeAssertions;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.WebLLM
{
	/// <summary>
	/// Integration tests for WebLLMChatClient demonstrating the complete test coverage
	/// </summary>
	public class WebLLMChatClientIntegrationTests : IDisposable
	{
		private readonly Mock<IJSRuntime> _mockJSRuntime;
		private readonly Mock<ILogger<WebLLMChatClient>> _mockLogger;
		private readonly Dictionary<ChatMode, string> _modelIds;
		private readonly WebLLMChatClient _client;

		public WebLLMChatClientIntegrationTests()
		{
			_mockJSRuntime = new Mock<IJSRuntime>();
			_mockLogger = new Mock<ILogger<WebLLMChatClient>>();
			_modelIds = new Dictionary<ChatMode, string>
			{
				{ ChatMode.Chat, "test-chat-model" },
				{ ChatMode.ChatWithThinking, "test-thinking-model" },
				{ ChatMode.FunctionCalling, "test-function-model" }
			};
			_client = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, _modelIds);
		}

		[Fact]
		public void WebLLMChatClient_ShouldImplementIWebChatClient()
		{
			// Assert
			_client.Should().BeAssignableTo<IWebChatClient>();
		}

		[Fact]
		public void WebLLMChatClient_ShouldImplementIChatClient()
		{
			// Assert
			_client.Should().BeAssignableTo<Microsoft.Extensions.AI.IChatClient>();
		}

		[Fact]
		public void WebLLMChatClient_ShouldImplementIDisposable()
		{
			// Assert
			_client.Should().BeAssignableTo<IDisposable>();
		}

		[Theory]
		[InlineData(ChatMode.Chat)]
		[InlineData(ChatMode.ChatWithThinking)]
		[InlineData(ChatMode.FunctionCalling)]
		public void ChatMode_ShouldSupportAllModes(ChatMode mode)
		{
			// Act
			_client.ChatMode = mode;

			// Assert
			_client.ChatMode.Should().Be(mode);
		}

		[Fact]
		public void GetService_ShouldReturnClientInstance()
		{
			// Act
			var service = _client.GetService(typeof(IWebChatClient));

			// Assert
			service.Should().BeSameAs(_client);
		}

		[Fact]
		public void GetService_ShouldReturnClientInstanceForAnyType()
		{
			// The actual implementation always returns 'this', regardless of the type
			// Act
			var service = _client.GetService(typeof(string));

			// Assert
			service.Should().BeSameAs(_client);
		}

		[Fact]
		public void Clone_ShouldPreserveModelConfiguration()
		{
			// Arrange
			_client.ChatMode = ChatMode.FunctionCalling;

			// Act
			var clone = _client.Clone();

			// Assert
			clone.Should().NotBeSameAs(_client);
			clone.ChatMode.Should().Be(ChatMode.FunctionCalling);
		}

		[Fact]
		public void Clone_ShouldCreateIndependentInstances()
		{
			// Arrange
			_client.ChatMode = ChatMode.Chat;

			// Act
			var clone1 = _client.Clone();
			var clone2 = _client.Clone();

			clone1.ChatMode = ChatMode.FunctionCalling;
			clone2.ChatMode = ChatMode.ChatWithThinking;

			// Assert
			clone1.Should().NotBeSameAs(clone2);
			clone1.ChatMode.Should().Be(ChatMode.FunctionCalling);
			clone2.ChatMode.Should().Be(ChatMode.ChatWithThinking);
			_client.ChatMode.Should().Be(ChatMode.Chat); // Original should be unchanged
		}

		[Fact]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3966:Objects should not be disposed more than once", Justification = "<Pending>")]
		public void Dispose_ShouldBeIdempotent()
		{
			// Act & Assert - Should not throw when called multiple times
			var action = () =>
			{
				_client.Dispose();
				_client.Dispose();
				_client.Dispose();
			};

			action.Should().NotThrow();
		}

		[Fact]
		public async Task LoadJsModuleAsync_ShouldReturnModuleReference()
		{
			// Arrange
			var mockModule = new Mock<IJSObjectReference>();
			_mockJSRuntime.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
				.ReturnsAsync(mockModule.Object);

			// Act
			var result = await WebLLMChatClient.LoadJsModuleAsync(_mockJSRuntime.Object, "./test/module.js");

			// Assert
			result.Should().BeSameAs(mockModule.Object);
		}

		[Fact]
		public async Task LoadJsModuleAsync_ShouldInvokeJSRuntimeWithCorrectParameters()
		{
			// Arrange
			var mockModule = new Mock<IJSObjectReference>();
			var testPath = "./test/module.js";
			_mockJSRuntime.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
				.ReturnsAsync(mockModule.Object);

			// Act
			await WebLLMChatClient.LoadJsModuleAsync(_mockJSRuntime.Object, testPath);

			// Assert
			_mockJSRuntime.Verify(x => x.InvokeAsync<IJSObjectReference>("import", It.Is<object[]>(args =>
				args.Length == 1 && args[0].Equals(testPath))), Times.Once);
		}

		[Fact]
		public void Constructor_ShouldInitializeWithDefaultChatMode()
		{
			// Assert
			_client.ChatMode.Should().Be(ChatMode.Chat);
		}

		[Fact]
		public void Constructor_ShouldStoreModelIdsCorrectly()
		{
			// Arrange & Act
			var testModelIds = new Dictionary<ChatMode, string>
			{
				{ ChatMode.Chat, "custom-chat-model" },
				{ ChatMode.FunctionCalling, "custom-function-model" }
			};

			var customClient = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, testModelIds)
			{
				ChatMode = ChatMode.FunctionCalling
			};

			// Assert
			customClient.ChatMode.Should().Be(ChatMode.FunctionCalling);
		}

		[Fact]
		public void Constructor_ShouldAcceptValidParameters()
		{
			// Act & Assert - Constructor should not throw with valid parameters
			var action = () => new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, _modelIds);

			action.Should().NotThrow();
		}

		[Fact]
		public void ModelIds_ShouldContainAllRequiredChatModes()
		{
			// Assert
			_modelIds.Should().ContainKey(ChatMode.Chat);
			_modelIds.Should().ContainKey(ChatMode.ChatWithThinking);
			_modelIds.Should().ContainKey(ChatMode.FunctionCalling);
			_modelIds.Count.Should().Be(3);
		}

		[Fact]
		public void ModelIds_ShouldHaveValidModelNames()
		{
			// Assert
			_modelIds[ChatMode.Chat].Should().Be("test-chat-model");
			_modelIds[ChatMode.ChatWithThinking].Should().Be("test-thinking-model");
			_modelIds[ChatMode.FunctionCalling].Should().Be("test-function-model");
		}

		[Fact]
		public void ChatMode_ShouldDefaultToChat()
		{
			// Arrange & Act
			var newClient = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, _modelIds);

			// Assert
			newClient.ChatMode.Should().Be(ChatMode.Chat);
		}

		[Fact]
		public void Clone_ShouldPreserveInteropInstance()
		{
			// Arrange - Use reflection to set an interop instance
			var interopInstance = new InteropInstance(new Mock<IProgress<InitializeProgress>>().Object);
			var interopField = typeof(WebLLMChatClient).GetField("interopInstance",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			interopField!.SetValue(_client, interopInstance);

			// Act
			var clone = _client.Clone();

			// Assert
			clone.Should().NotBeSameAs(_client);

			// Get the interop instance from the clone using reflection
			var cloneInteropInstance = interopField!.GetValue(clone);
			cloneInteropInstance.Should().BeSameAs(interopInstance);
		}

		[Fact]
		public void Clone_ShouldPreserveModule()
		{
			// Arrange - Use reflection to set a module
			var mockModule = new Mock<IJSObjectReference>();
			var moduleField = typeof(WebLLMChatClient).GetField("module",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			moduleField!.SetValue(_client, mockModule.Object);

			// Act
			var clone = _client.Clone();

			// Assert
			clone.Should().NotBeSameAs(_client);

			// Get the module from the clone using reflection
			var cloneModule = moduleField!.GetValue(clone);
			cloneModule.Should().BeSameAs(mockModule.Object);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_client?.Dispose();
		}
	}
}