using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.WebLLM
{
	/// <summary>
	/// Tests for streaming functionality of WebLLMChatClient
	/// </summary>
	public class WebLLMChatClientStreamingTests : IDisposable
	{
		private readonly Mock<IJSRuntime> _mockJSRuntime;
		private readonly Mock<ILogger<WebLLMChatClient>> _mockLogger;
		private readonly Mock<IJSObjectReference> _mockModule;
		private readonly Dictionary<ChatMode, string> _modelIds;
		private readonly WebLLMChatClient _client;

		public WebLLMChatClientStreamingTests()
		{
			_mockJSRuntime = new Mock<IJSRuntime>();
			_mockLogger = new Mock<ILogger<WebLLMChatClient>>();
			_mockModule = new Mock<IJSObjectReference>();
			_modelIds = new Dictionary<ChatMode, string>
			{
				{ ChatMode.Chat, "test-chat-model" }
			};
			_client = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, _modelIds);

			// Setup JS runtime to return our mock module
			_mockJSRuntime.Setup(x => x.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
				.ReturnsAsync(_mockModule.Object);
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WithEmptyMessages_ShouldReturnEmptyResponse()
		{
			// Arrange
			var messages = Array.Empty<ChatMessage>();

			// Act & Assert - Should throw NotSupportedException since client is not initialized
			await Assert.ThrowsAsync<NotSupportedException>(async () =>
			{
				await foreach (var response in _client.GetStreamingResponseAsync(messages))
				{
					// Should not reach here
				}
			});
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
		public async Task InitializeAsync_ShouldCallJSModule()
		{
			// Arrange
			var progress = new Mock<IProgress<InitializeProgress>>();

			// Act
			await _client.InitializeAsync(progress.Object);

			// Assert
			_mockJSRuntime.Verify(x => x.InvokeAsync<IJSObjectReference>("import", 
				It.Is<object[]>(args => args.Length == 1 && args[0].ToString() == "./js/dist/webllm.interop.js")), 
				Times.Once);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_client?.Dispose();
		}
	}
}