using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using Moq;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class AgentOrchestratorTests
	{
		private readonly Mock<ICustomChatClient> _mockWebChatClient;
		private readonly TestServiceProvider _serviceProvider;
		private readonly GoogleSearchService _google_search_service;
		private readonly AgentLogger _agentLogger;

		public AgentOrchestratorTests()
		{
			_mockWebChatClient = new Mock<ICustomChatClient>();

			var context = new GoogleSearchContext
			{
				HttpClient = new HttpClient(),
				ApiKey = "test-key",
				CustomSearchEngineId = "test-cx"
			};
			_google_search_service = new GoogleSearchService(context);

			_agentLogger = new AgentLogger();

			_serviceProvider = new TestServiceProvider();
			_serviceProvider.AddService(_google_search_service);
			_serviceProvider.AddService(_agentLogger);
			_serviceProvider.AddService(new ModelInfo { MaxTokens = 4096, Name = "123" });
			_serviceProvider.AddService(_mockWebChatClient.Object);

			var clonedClient = new Mock<ICustomChatClient>();
			clonedClient.SetupProperty(x => x.ChatMode);
			_mockWebChatClient.Setup(x => x.Clone()).Returns(clonedClient.Object);
		}

		[Fact]
		public void Constructor_ShouldNotThrow()
		{
			// Arrange & Act
			var orchestrator = new AgentOrchestrator(_serviceProvider, _agentLogger);

			// Assert
			Assert.NotNull(orchestrator);
		}

		[Fact]
		public async Task AskQuestion_ReturnsAsyncEnumerable_CanGetEnumeratorAndDispose()
		{
			// Arrange
			var orchestrator = new AgentOrchestrator(_serviceProvider, _agentLogger);

			// Act
			var asyncEnumerable = orchestrator.AskQuestion("Test question");

			// Assert basic contract: not null and we can get an enumerator and dispose it without iterating.
			Assert.NotNull(asyncEnumerable);

			await using var enumerator = asyncEnumerable.GetAsyncEnumerator(CancellationToken.None);
			// Do not call MoveNextAsync to avoid invoking downstream services. We just ensure enumerator can be acquired and disposed.
		}

		[Fact]
		public async Task History_ShouldReturnEmptyCollection_WhenNoConversation()
		{
			// Arrange
			var orchestrator = new AgentOrchestrator(_serviceProvider, _agentLogger);

			// Act
			var history = await orchestrator.History();

			// Assert
			Assert.NotNull(history);
			Assert.Empty(history);
		}
	}
}
