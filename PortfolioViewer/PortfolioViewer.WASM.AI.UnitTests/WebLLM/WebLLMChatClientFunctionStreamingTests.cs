using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.WebLLM
{
	/// <summary>
	/// Tests for streaming with function calling functionality
	/// </summary>
	public class WebLLMChatClientFunctionStreamingTests : IDisposable
	{
		private readonly Mock<IJSRuntime> _mockJSRuntime;
		private readonly Mock<ILogger<WebLLMChatClient>> _mockLogger;
		private readonly Dictionary<ChatMode, string> _modelIds;
		private readonly WebLLMChatClient _client;

		public WebLLMChatClientFunctionStreamingTests()
		{
			_mockJSRuntime = new Mock<IJSRuntime>();
			_mockLogger = new Mock<ILogger<WebLLMChatClient>>();
			_modelIds = new Dictionary<ChatMode, string>
			{
				{ ChatMode.FunctionCalling, "test-function-model" }
			};
			_client = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, _modelIds)
			{
				ChatMode = ChatMode.FunctionCalling
			};
		}

		[Fact]
		public void Constructor_WithFunctionCallingMode_ShouldSetCorrectMode()
		{
			// Assert
			Assert.Equal(ChatMode.FunctionCalling, _client.ChatMode);
		}

		[Fact]
		public void ChatMode_ShouldAllowSettingToFunctionCalling()
		{
			// Act
			_client.ChatMode = ChatMode.FunctionCalling;

			// Assert
			Assert.Equal(ChatMode.FunctionCalling, _client.ChatMode);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_client?.Dispose();
		}
	}
}