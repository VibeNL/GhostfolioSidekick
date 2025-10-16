using GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.Agents
{
	public class AgentOrchestratorTests
	{
		private readonly Mock<IWebChatClient> _mockWebChatClient;
		private readonly TestServiceProvider _service_provider;
		private readonly GoogleSearchService _google_search_service;
		private readonly AgentLogger _agentLogger;

		public AgentOrchestratorTests()
		{
			_mockWebChatClient = new Mock<IWebChatClient>();

			var context = new GoogleSearchContext
			{
				HttpClient = new HttpClient(),
				ApiKey = "test-key",
				CustomSearchEngineId = "test-cx"
			};
			_google_search_service = new GoogleSearchService(context);

			_agentLogger = new AgentLogger();

			_service_provider = new TestServiceProvider();
			_service_provider.AddService(_google_search_service);
			_service_provider.AddService(_agentLogger);
			// Register the mock web chat client so AgentOrchestrator can resolve it
			_service_provider.AddService(_mockWebChatClient.Object);

			var clonedClient = new Mock<IWebChatClient>();
			clonedClient.SetupProperty(x => x.ChatMode);
			_mockWebChatClient.Setup(x => x.Clone()).Returns(clonedClient.Object);

			// Make sure ServiceProvider.GetRequiredService is available via extension in tests
			// (TestServiceProvider implements GetRequiredService used by code under test)
		}

		[Fact]
		public void Constructor_ShouldNotThrow()
		{
			// Arrange & Act - use the real constructor that depends on service provider
			var orchestrator = new AgentOrchestrator(_service_provider, _agentLogger);

			// Assert
			Assert.NotNull(orchestrator);
		}

		[Fact]
		public async Task History_ShouldContainAddedUserMessage()
		{
			// Arrange - use shim and inject into ctor
			var shim = new TestShim();
			var orchestrator = new AgentOrchestrator(shim, _agentLogger);

			// Act - add a message via shim and read history
			shim.AddChatMessage(new SimpleStreamingMessage(AuthorRole.User, "Hello from test", "User"));

			var history = await orchestrator.History();

			// Assert
			Assert.Contains(history, m => (m.Content ?? string.Empty).Contains("Hello from test"));
		}

		[Fact]
		public async Task AskQuestion_ReturnsAsyncEnumerable_CanGetEnumeratorAndDispose()
		{
			// Arrange - inject shim
			var shim = new TestShim();
			var orchestrator = new AgentOrchestrator(shim, _agentLogger);

			// Act
			var asyncEnumerable = orchestrator.AskQuestion("Test question");

			// Assert basic contract: not null and we can get an enumerator and dispose it without iterating.
			Assert.NotNull(asyncEnumerable);

			await using var enumerator = asyncEnumerable.GetAsyncEnumerator();
			// Do not call MoveNextAsync to avoid invoking downstream services. We just ensure enumerator can be acquired and disposed.
		}

		[Fact]
		public async Task AskQuestion_StreamsMockedResponses()
		{
			// Arrange - create shim that yields predictable streaming messages
			var shim = new TestShim();
			var orchestrator = new AgentOrchestrator(shim, _agentLogger);

			// Act - collect streaming responses
			var builder = new System.Text.StringBuilder();
			await foreach (var update in orchestrator.AskQuestion("Hello"))
			{
				builder.Append(update.Content);
			}

			// Assert
			var result = builder.ToString();
			Assert.Contains("Part1", result);
			Assert.Contains("Part2", result);
		}

		// Test shim that implements the AgentOrchestrator.IAgentGroupChatShim interface
		private class TestShim : IAgentGroupChatShim
		{
			private readonly List<SimpleStreamingMessage> _messages = [];

			public void AddChatMessage(SimpleStreamingMessage message)
			{
				_messages.Add(message);
			}

			public async IAsyncEnumerable<SimpleStreamingMessage> InvokeStreamingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				// Simulate streaming by yielding two parts
				yield return new SimpleStreamingMessage(AuthorRole.Assistant, "Part1", "Tester");
				await Task.Yield();
				yield return new SimpleStreamingMessage(AuthorRole.Assistant, "Part2", "Tester");
			}

			public async IAsyncEnumerable<SimpleStreamingMessage> GetChatMessagesAsync()
			{
				foreach (var m in _messages)
				{
					yield return m;
				}
				await Task.CompletedTask;
			}
		}
	}
}
