using System.Net;
using System.Net.Http.Json;
using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.Api;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests
{
	public class ApiChatClientTests
	{
		private static ApiChatClient CreateClient(HttpMessageHandler? handler = null)
		{
			var httpClient = handler != null ? new HttpClient(handler) { BaseAddress = new Uri("http://localhost") } : new HttpClient() { BaseAddress = new Uri("http://localhost") };
			var logger = Mock.Of<ILogger<ApiChatClient>>();
			return new ApiChatClient(httpClient, logger);
		}

		[Fact]
		public async Task GetResponseAsync_ReturnsChatResponse()
		{
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = JsonContent.Create(new { Text = "Hello!" })
			};
			var handler = new Mock<HttpMessageHandler>();
			handler.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);
			var client = CreateClient(handler.Object);
			var messages = new[] { new ChatMessage(ChatRole.User, "Hi") };
			var result = await client.GetResponseAsync(messages, cancellationToken: CancellationToken.None);

			var message = result.Messages.SingleOrDefault();

			Assert.NotNull(message);
			Assert.Equal("Hello!", message!.Text);
			Assert.Equal(ChatRole.Assistant, message.Role);
		}

		[Fact]
		public async Task GetStreamingResponseAsync_YieldsSingleUpdate()
		{
			var response = new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = JsonContent.Create(new { Text = "Streamed!" })
			};
			var handler = new Mock<HttpMessageHandler>();
			handler.Protected()
			.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
			.ReturnsAsync(response);
			var client = CreateClient(handler.Object);
			var messages = new[] { new ChatMessage(ChatRole.User, "Hi") };
			var updates = new List<ChatResponseUpdate>();
			await foreach (var update in client.GetStreamingResponseAsync(messages, cancellationToken: CancellationToken.None))
			{
				updates.Add(update);
			}
			Assert.Single(updates);
			Assert.Equal("Streamed!", updates[0].Text);
			Assert.Equal(ChatRole.Assistant, updates[0].Role);
		}

		[Fact]
		public void Clone_ReturnsNewInstanceWithSameChatMode()
		{
			var client = CreateClient();
			client.ChatMode = ChatMode.FunctionCalling;
			var clone = client.Clone();
			Assert.NotSame(client, clone);
			Assert.Equal(client.ChatMode, clone.ChatMode);
		}

		[Fact]
		public void GetService_ReturnsSelf()
		{
			var client = CreateClient();
			var service = client.GetService(typeof(ApiChatClient));
			Assert.Same(client, service);
		}

		[Fact]
		public async Task InitializeAsync_ReportsProgress()
		{
			var client = CreateClient();
			bool reported = false;
			await client.InitializeAsync(new Progress<InitializeProgress>(p => { reported = true; Assert.Equal(1.0, p.Progress); }));
			await Task.Delay(100, CancellationToken.None); // Ensure progress callback has time to run
			Assert.True(reported);
		}

		[Fact]
		public void Dispose_CanBeCalledMultipleTimesSafely()
		{
			var client = CreateClient();
			client.Dispose();
			Assert.True(true); // No exception means success
		}
	}
}
