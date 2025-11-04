using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.WebLLM
{
	/// <summary>
	/// Tests for threading and concurrency scenarios
	/// </summary>
	public class WebLLMChatClientConcurrencyTests : IDisposable
	{
		private readonly Mock<IJSRuntime> _mockJSRuntime;
		private readonly Mock<ILogger<WebLLMChatClient>> _mockLogger;
		private readonly Dictionary<ChatMode, string> _modelIds;
		private readonly WebLLMChatClient _client;

		public WebLLMChatClientConcurrencyTests()
		{
			_mockJSRuntime = new Mock<IJSRuntime>();
			_mockLogger = new Mock<ILogger<WebLLMChatClient>>();
			_modelIds = new Dictionary<ChatMode, string> { { ChatMode.Chat, "test-model" } };
			_client = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, _modelIds);
		}

		[Fact]
		public void WebLLMCompletions_Queue_ShouldBeConcurrentQueue()
		{
			// This verifies that the InteropInstance uses a thread-safe queue
			var interop = new InteropInstance(new Mock<IProgress<InitializeProgress>>().Object);
			Assert.IsType<System.Collections.Concurrent.ConcurrentQueue<WebLLMCompletion>>(interop.WebLLMCompletions);
		}

		[Fact]
		public void MultipleClones_ShouldBeIndependent()
		{
			// Arrange
			var clone1 = _client.Clone();
			var clone2 = _client.Clone();

			// Act
			clone1.ChatMode = ChatMode.Chat;
			clone2.ChatMode = ChatMode.FunctionCalling;

			// Assert
			Assert.NotSame(clone1, clone2);
			Assert.NotSame(_client, clone1);
			Assert.NotSame(_client, clone2);
			Assert.Equal(ChatMode.Chat, clone1.ChatMode);
			Assert.Equal(ChatMode.FunctionCalling, clone2.ChatMode);
		}

		[Fact]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S2699:Tests should include assertions", Justification = "<Pending>")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3966:Objects should not be disposed more than once", Justification = "<Pending>")]
		public void Dispose_CalledMultipleTimes_ShouldNotThrow()
		{
			// Act & Assert - Should not throw
			_client.Dispose();
			_client.Dispose();
			_client.Dispose();
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_client?.Dispose();
		}
	}
}