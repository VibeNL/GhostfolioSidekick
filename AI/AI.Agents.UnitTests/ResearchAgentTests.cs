using GhostfolioSidekick.AI.Common;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using Microsoft.Agents.AI;
using Moq;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class ResearchAgentTests
	{
		private readonly Mock<ICustomChatClient> _mockWebChatClient;
		private readonly TestServiceProvider _serviceProvider;
		private readonly GoogleSearchService _googleSearchService;
		private readonly AgentLogger _agentLogger;

		public ResearchAgentTests()
		{
			_mockWebChatClient = new Mock<ICustomChatClient>();

			var context = new GoogleSearchContext
			{
				HttpClient = new HttpClient(),
				ApiKey = "test-key",
				CustomSearchEngineId = "test-cx"
			};
			_googleSearchService = new GoogleSearchService(context);
			_agentLogger = new AgentLogger();

			_serviceProvider = new TestServiceProvider();
			_serviceProvider.AddService(_googleSearchService);
			_serviceProvider.AddService(_agentLogger);
			_serviceProvider.AddService(new ModelInfo { MaxTokens = 4096, Name = "123" });

			var clonedClient = new Mock<ICustomChatClient>();
			clonedClient.SetupProperty(x => x.ChatMode);
			_mockWebChatClient.Setup(x => x.Clone()).Returns(clonedClient.Object);
		}

		[Fact]
		public void Create_ShouldReturnChatClientAgent()
		{
			// Act
			var agent = ResearchAgent.Create(_mockWebChatClient.Object, _serviceProvider);

			// Assert
			Assert.NotNull(agent);
			Assert.IsType<ChatClientAgent>(agent);
			Assert.Equal("ResearchAgent", agent.Name);
			Assert.Contains("ResearchAgent AI", agent.Instructions);
			Assert.Contains("smart financial assistant", agent.Instructions);
			Assert.NotNull(agent.Description);
			Assert.Contains("researcher", agent.Description);
			Assert.Contains("real-time data", agent.Description);
		}

		[Fact]
		public void Create_ShouldIncludeCurrentDateInSystemPrompt()
		{
			// Act
			var agent = ResearchAgent.Create(_mockWebChatClient.Object, _serviceProvider);

			// Assert
			var expectedDate = DateTime.Now.ToString("yyyy-MM-dd");
			Assert.Contains($"Today is {expectedDate}", agent.Instructions);
		}

		[Fact]
		public void Create_ShouldConfigureAgentWithCorrectDescription()
		{
			// Act
			var agent = ResearchAgent.Create(_mockWebChatClient.Object, _serviceProvider);

			// Assert
			Assert.Equal("A researcher that can access real-time data on the internet. Also can query recent financial news and perform multi-step research.", agent.Description);
		}
	}

	// Test service provider that implements IServiceProvider without using extension methods
	public class TestServiceProvider : IServiceProvider
	{
		private readonly Dictionary<Type, object> _services = [];

		public void AddService<T>(T service) where T : class
		{
			_services[typeof(T)] = service;
		}

		public object? GetService(Type serviceType)
		{
			return _services.TryGetValue(serviceType, out var service) ? service : null;
		}

		public T GetRequiredService<T>() where T : class
		{
			var service = GetService(typeof(T));
			return service as T ?? throw new InvalidOperationException($"Service of type {typeof(T)} not found.");
		}
	}
}
