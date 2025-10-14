using GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents;
using GhostfolioSidekick.PortfolioViewer.WASM.AI.OnlineSearch;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using System.ComponentModel;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.UnitTests.Agents
{
	public class ResearchAgentTests
	{
		private readonly Mock<IWebChatClient> _mockWebChatClient;
		private readonly TestServiceProvider _serviceProvider;
		private readonly GoogleSearchService _googleSearchService;
		private readonly AgentLogger _agentLogger;

		public ResearchAgentTests()
		{
			_mockWebChatClient = new Mock<IWebChatClient>();
			
			// Use the constructor with GoogleSearchContext to avoid optional parameters
			var context = new GoogleSearchContext
			{
				HttpClient = new HttpClient(),
				ApiKey = "test-key",
				CustomSearchEngineId = "test-cx"
			};
			_googleSearchService = new GoogleSearchService(context);
			_agentLogger = new AgentLogger();

			// Create a test service provider that avoids the GetRequiredService extension method
			_serviceProvider = new TestServiceProvider();
			_serviceProvider.AddService<GoogleSearchService>(_googleSearchService);
			_serviceProvider.AddService<AgentLogger>(_agentLogger);

			// Setup the web chat client clone behavior
			var clonedClient = new Mock<IWebChatClient>();
			clonedClient.SetupProperty(x => x.ChatMode);
			_mockWebChatClient.Setup(x => x.Clone()).Returns(clonedClient.Object);
		}

		[Fact]
		public void Create_ShouldReturnChatCompletionAgent()
		{
			// Act
			var agent = ResearchAgent.Create(_mockWebChatClient.Object, _serviceProvider);

			// Assert
			Assert.NotNull(agent);
			Assert.IsType<ChatCompletionAgent>(agent);
			Assert.Equal("ResearchAgent", agent.Name);
			Assert.Contains("ResearchAgent AI", agent.Instructions);
			Assert.Contains("smart financial assistant", agent.Instructions);
			Assert.Equal(AuthorRole.System, agent.InstructionsRole);
			Assert.NotNull(agent.Kernel);
			Assert.NotNull(agent.Description);
			Assert.Contains("researcher", agent.Description);
			Assert.Contains("real-time data", agent.Description);
		}

		[Fact]
		public void Create_ShouldSetupKernelWithPlugins()
		{
			// Act
			var agent = ResearchAgent.Create(_mockWebChatClient.Object, _serviceProvider);

			// Assert
			Assert.NotNull(agent.Kernel);
			Assert.True(agent.Kernel.Plugins.Count > 0);
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
		public void Create_ShouldSetupFunctionChoiceBehavior()
		{
			// Act
			var agent = ResearchAgent.Create(_mockWebChatClient.Object, _serviceProvider);

			// Assert
			Assert.NotNull(agent.Arguments);
			Assert.IsType<KernelArguments>(agent.Arguments);
		}

		[Fact]
		public void Create_ShouldConfigureAgentWithCorrectDescription()
		{
			// Act
			var agent = ResearchAgent.Create(_mockWebChatClient.Object, _serviceProvider);

			// Assert
			Assert.Equal("A researcher that can access real-time data on the internet. Also can query recent financial news and perform multi-step research.", agent.Description);
		}

		[Fact]
		public void Create_ShouldAddResearchAgentFunctionToKernel()
		{
			// Act
			var agent = ResearchAgent.Create(_mockWebChatClient.Object, _serviceProvider);

			// Assert
			Assert.NotNull(agent.Kernel);
			Assert.True(agent.Kernel.Plugins.Count > 0);
			
			// Verify that the kernel has plugins (ResearchAgentFunction was added)
			var hasPlugins = agent.Kernel.Plugins.Count != 0;
			Assert.True(hasPlugins);
		}
	}

	public class ResearchAgentFunctionTests
	{
		private readonly GoogleSearchService _googleSearchService;
		private readonly Mock<IChatCompletionService> _mockChatService;
		private readonly AgentLogger _agentLogger;
		private readonly ResearchAgentFunction _researchAgentFunction;

		public ResearchAgentFunctionTests()
		{
			// Use the constructor with GoogleSearchContext to avoid optional parameters
			var context = new GoogleSearchContext
			{
				HttpClient = new HttpClient(),
				ApiKey = "test-key",
				CustomSearchEngineId = "test-cx"
			};
			_googleSearchService = new GoogleSearchService(context);
			_mockChatService = new Mock<IChatCompletionService>();
			_agentLogger = new AgentLogger();
			_researchAgentFunction = new ResearchAgentFunction(_googleSearchService, _mockChatService.Object, _agentLogger);
		}

		[Fact]
		public async Task MultiStepResearch_WithEmptyTopic_ShouldReturnErrorMessage()
		{
			// Arrange
			var topic = "";
			var aspects = new[] { "overview" };

			// Act
			var result = await _researchAgentFunction.MultiStepResearch(topic, aspects);

			// Assert
			Assert.Equal("No research topic provided.", result);
		}

		[Fact]
		public async Task MultiStepResearch_WithNullTopic_ShouldReturnErrorMessage()
		{
			// Arrange
			string? topic = null;
			var aspects = new[] { "overview" };

			// Act
			var result = await _researchAgentFunction.MultiStepResearch(topic!, aspects);

			// Assert
			Assert.Equal("No research topic provided.", result);
		}

		[Fact]
		public async Task MultiStepResearch_WithWhitespaceTopic_ShouldReturnErrorMessage()
		{
			// Arrange
			var topic = "   ";
			var aspects = new[] { "overview" };

			// Act
			var result = await _researchAgentFunction.MultiStepResearch(topic, aspects);

			// Assert
			Assert.Equal("No research topic provided.", result);
		}

		[Fact]
		public void MultiStepResearch_ShouldHaveCorrectKernelFunctionAttribute()
		{
			// Arrange
			var method = typeof(ResearchAgentFunction).GetMethod(nameof(ResearchAgentFunction.MultiStepResearch));

			// Act
			var kernelFunctionAttribute = method?.GetCustomAttributes(typeof(KernelFunctionAttribute), false).FirstOrDefault() as KernelFunctionAttribute;
			var descriptionAttribute = method?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;

			// Assert
			Assert.NotNull(kernelFunctionAttribute);
			Assert.Equal("multi_step_research", kernelFunctionAttribute.Name);
			Assert.NotNull(descriptionAttribute);
			Assert.Contains("multi-step research", descriptionAttribute.Description);
		}

		[Fact]
		public void MultiStepResearch_ParametersShouldHaveCorrectDescriptions()
		{
			// Arrange
			var method = typeof(ResearchAgentFunction).GetMethod(nameof(ResearchAgentFunction.MultiStepResearch));
			var parameters = method?.GetParameters();

			// Act & Assert
			Assert.NotNull(parameters);
			Assert.Equal(2, parameters.Length);

			var topicParam = parameters[0];
			var aspectsParam = parameters[1];

			var topicDescription = topicParam.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
			var aspectsDescription = aspectsParam.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;

			Assert.NotNull(topicDescription);
			Assert.Equal("The topic to research", topicDescription.Description);

			Assert.NotNull(aspectsDescription);
			Assert.Contains("Specific aspects", aspectsDescription.Description);
			Assert.Contains("natural language", aspectsDescription.Description);
		}
	}

	public class SearchResultsTests
	{
		[Fact]
		public void SearchResults_ShouldInitializeWithDefaults()
		{
			// Act
			var searchResults = new SearchResults();

			// Assert
			Assert.Equal(string.Empty, searchResults.Query);
			Assert.NotNull(searchResults.Items);
			Assert.Empty(searchResults.Items);
		}

		[Fact]
		public void SearchResults_ShouldAllowSettingProperties()
		{
			// Arrange
			var items = new List<SearchResultItem>
			{
				new() { Title = "Test Title", Link = "https://test.com", Content = "Test content" }
			};

			// Act
			var searchResults = new SearchResults
			{
				Query = "test query",
				Items = items
			};

			// Assert
			Assert.Equal("test query", searchResults.Query);
			Assert.Single(searchResults.Items);
			Assert.Equal("Test Title", searchResults.Items[0].Title);
		}
	}

	public class SearchResultItemTests
	{
		[Fact]
		public void SearchResultItem_ShouldInitializeWithDefaults()
		{
			// Act
			var item = new SearchResultItem();

			// Assert
			Assert.Equal(string.Empty, item.Title);
			Assert.Equal(string.Empty, item.Link);
			Assert.Equal(string.Empty, item.Content);
		}

		[Fact]
		public void SearchResultItem_ShouldAllowSettingProperties()
		{
			// Act
			var item = new SearchResultItem
			{
				Title = "Test Title",
				Link = "https://example.com",
				Content = "Test content here"
			};

			// Assert
			Assert.Equal("Test Title", item.Title);
			Assert.Equal("https://example.com", item.Link);
			Assert.Equal("Test content here", item.Content);
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