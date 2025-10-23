using Moq;
using System.Reflection;

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
				HttpClient = new System.Net.Http.HttpClient(),
				ApiKey = "test-key",
				CustomSearchEngineId = "test-cx"
			};
			_google_search_service = new GoogleSearchService(context);

			_agentLogger = new AgentLogger();

			_service_provider = new TestServiceProvider();
			_service_provider.AddService<GoogleSearchService>(_google_search_service);
			_service_provider.AddService<AgentLogger>(_agentLogger);
			// Register the mock web chat client so AgentOrchestrator can resolve it
			_service_provider.AddService<IWebChatClient>(_mockWebChatClient.Object);

			var clonedClient = new Mock<IWebChatClient>();
			clonedClient.SetupProperty(x => x.ChatMode);
			_mockWebChatClient.Setup(x => x.Clone()).Returns(clonedClient.Object);

			// Make sure ServiceProvider.GetRequiredService is available via extension in tests
			// (TestServiceProvider implements GetRequiredService used by code under test)
		}

		[Fact]
		public void Constructor_ShouldNotThrow()
		{
			// Arrange & Act
			var orchestrator = new AgentOrchestrator(_service_provider, _agentLogger);

			// Assert
			Assert.NotNull(orchestrator);
		}

		[Fact]
		public async System.Threading.Tasks.Task History_ShouldContainAddedUserMessage()
		{
			// Arrange
			var orchestrator = new AgentOrchestrator(_service_provider, _agentLogger);

			// Use reflection to get the private groupChat field
			var field = typeof(AgentOrchestrator).GetField("groupChat", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull(field);
			var groupChat = field!.GetValue(orchestrator);
			Assert.NotNull(groupChat);

			// Call AddChatMessage on the groupChat instance
			var addMethod = groupChat!.GetType().GetMethod("AddChatMessage", BindingFlags.Public | BindingFlags.Instance);
			Assert.NotNull(addMethod);

			// Create a ChatMessageContent instance via reflection to avoid compile-time dependency
			var paramType = addMethod!.GetParameters()[0].ParameterType;

			// Find a constructor with (something, string) signature
			var ctor = paramType.GetConstructors().FirstOrDefault(c =>
			{
				var ps = c.GetParameters();
				return ps.Length == 2 && ps[1].ParameterType == typeof(string);
			});

			object chatMessageInstance;
			if (ctor != null)
			{
				var roleType = ctor.GetParameters()[0].ParameterType;
				object roleValue;
				if (roleType.IsEnum)
				{
					roleValue = Enum.GetValues(roleType).GetValue(0)!;
				}
				else if (roleType == typeof(int))
				{
					roleValue = 0;
				}
				else
				{
					roleValue = Activator.CreateInstance(roleType)!;
				}

				chatMessageInstance = ctor.Invoke([roleValue, "Hello from test"])!;
			}
			else
			{
				// As a fallback try parameterless constructor and set properties
				chatMessageInstance = Activator.CreateInstance(paramType)!;
				var contentProp = paramType.GetProperty("Content");
				if (contentProp != null && contentProp.CanWrite)
				{
					contentProp.SetValue(chatMessageInstance, "Hello from test");
				}
			}

			// Set AuthorName if available
			var authorNameProp = paramType.GetProperty("AuthorName");
			if (authorNameProp != null && authorNameProp.CanWrite)
			{
				authorNameProp.SetValue(chatMessageInstance, "User");
			}

			addMethod.Invoke(groupChat, [chatMessageInstance]);

			// Act
			var history = await orchestrator.History();

			// Assert
			Assert.Contains(history, m => (m.Content ?? string.Empty).Contains("Hello from test"));
		}

		[Fact]
		public async System.Threading.Tasks.Task AskQuestion_ReturnsAsyncEnumerable_CanGetEnumeratorAndDispose()
		{
			// Arrange
			var orchestrator = new AgentOrchestrator(_service_provider, _agentLogger);

			// Act
			var asyncEnumerable = orchestrator.AskQuestion("Test question");

			// Assert basic contract: not null and we can get an enumerator and dispose it without iterating.
			Assert.NotNull(asyncEnumerable);

			await using (var enumerator = asyncEnumerable.GetAsyncEnumerator())
			{
				// Do not call MoveNextAsync to avoid invoking downstream services. We just ensure enumerator can be acquired and disposed.
			}
		}
	}
}
