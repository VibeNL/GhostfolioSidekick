using AwesomeAssertions;
using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using System.Reflection;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.WebLLM
{
	/// <summary>
	/// Tests that streaming failures surface as exceptions instead of hanging the consumer forever.
	/// </summary>
	public class WebLLMChatClientStreamErrorTests : IDisposable
	{
		private readonly Mock<IJSRuntime> _mockJSRuntime;
		private readonly Mock<ILogger<WebLLMChatClient>> _mockLogger;
		private readonly Mock<IJSObjectReference> _mockModule;
		private readonly WebLLMChatClient _client;

		public WebLLMChatClientStreamErrorTests()
		{
			_mockJSRuntime = new Mock<IJSRuntime>();
			_mockLogger = new Mock<ILogger<WebLLMChatClient>>();
			_mockModule = new Mock<IJSObjectReference>();

			var modelIds = new Dictionary<ChatMode, string> { { ChatMode.Chat, "chat-model" } };
			_client = new WebLLMChatClient(_mockJSRuntime.Object, _mockLogger.Object, modelIds);
		}

		private InteropInstance GetInteropInstance()
		{
			var field = typeof(WebLLMChatClient).GetField("interopInstance", BindingFlags.NonPublic | BindingFlags.Instance);
			return (InteropInstance)field!.GetValue(_client)!;
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WhenJsInteropFails_ThrowsInsteadOfHanging()
		{
			// Arrange: the JS module import succeeds, but starting the stream fails.
			_mockJSRuntime
				.Setup(js => js.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
				.ReturnsAsync(_mockModule.Object);
			_mockModule
				.Setup(m => m.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
				.ThrowsAsync(new JSException("boom"));

			var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // safety net so the test can never hang

			// Act & Assert: the stream must fail fast with the interop error as inner exception.
			var ex = await Assert.ThrowsAnyAsync<InvalidOperationException>(async () =>
			{
				await foreach (var _ in _client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")], cancellationToken: cts.Token))
				{
					// consume
				}
			});

			ex.InnerException.Should().BeOfType<JSException>();
		}

		[Fact]
		public async Task GetStreamingResponseAsync_WhenJsReportsStreamError_ThrowsWithReportedMessage()
		{
			// Arrange: the JS stream starts successfully but no chunks ever arrive; the JS side reports an error via ReportStreamError.
			_mockJSRuntime
				.Setup(js => js.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
				.ReturnsAsync(_mockModule.Object);
			_mockModule
				.Setup(m => m.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(It.IsAny<string>(), It.IsAny<object[]>()))
				.ReturnsAsync(default(Microsoft.JSInterop.Infrastructure.IJSVoidResult)!);

			var interopInstance = GetInteropInstance();

			var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // safety net so the test can never hang
			var streamTask = ConsumeStreamAsync(cts.Token);

			// Give the polling loop a moment to start, then report the error.
			await Task.Delay(50, cts.Token);
			interopInstance.ReportStreamError("engine exploded");

			// Act & Assert
			var ex = await Assert.ThrowsAnyAsync<InvalidOperationException>(async () => await streamTask);
			ex.Message.Should().Contain("WebLLM streaming failed.");
			ex.InnerException!.Message.Should().Contain("engine exploded");
		}

		private async Task ConsumeStreamAsync(CancellationToken cancellationToken)
		{
			await foreach (var _ in _client.GetStreamingResponseAsync([new ChatMessage(ChatRole.User, "hi")], cancellationToken: cancellationToken))
			{
				// consume
			}
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			_client?.Dispose();
		}
	}
}
