using GhostfolioSidekick.AI.Functions.OnlineSearch;
using GhostfolioSidekick.AI.Common;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Moq;

namespace GhostfolioSidekick.AI.Functions.UnitTests
{
	public class ResearchAgentFunctionTests
	{
		private readonly Mock<IGoogleSearchService> _searchServiceMock = new();
		private readonly Mock<AgentLogger> _loggerMock = new();
		private readonly Mock<IChatCompletionService> _chatService = new();

		[Fact]
		public async Task SummarizeSearchResults_ReturnsNoResultsMessage_WhenEmpty()
		{
			SetupChatService();
			var agent = new ResearchAgentFunction(_searchServiceMock.Object, _chatService.Object, _loggerMock.Object);
			var results = new SearchResults { Query = "test", Items = new List<SearchResultItem>() };
			var summary = await InvokePrivateAsync(agent, "SummarizeSearchResults", results);
			Assert.Equal("No results to summarize.", summary);
		}

		[Fact]
		public async Task SummarizeSearchResults_ReturnsSummary_WhenResultsExist()
		{
			SetupChatService(CreateChatMessage("summary"));
			var agent = new ResearchAgentFunction(_searchServiceMock.Object, _chatService.Object, _loggerMock.Object);
			var results = new SearchResults
			{
				Query = "test",
				Items = new List<SearchResultItem>
				{
					new SearchResultItem { Title = "Title1", Content = new string('a',200) },
					new SearchResultItem { Title = "Title2", Content = new string('b',200) }
				}
			};
			var summary = await InvokePrivateAsync(agent, "SummarizeSearchResults", results);
			Assert.Equal("summary", summary);
		}

		[Fact]
		public async Task GenerateSearchQuery_ReturnsOptimizedQuery()
		{
			SetupChatService(CreateChatMessage("topic aspect"));
			var agent = new ResearchAgentFunction(_searchServiceMock.Object, _chatService.Object, _loggerMock.Object);
			var result = await InvokePrivateAsync(agent, "GenerateSearchQuery", "topic", "aspect");
			Assert.Contains("topic", result);
			Assert.Contains("aspect", result);
			Assert.True(result.Length <=150);
		}

		[Fact]
		public async Task SuggestAlternativeQuery_ReturnsAlternativeQuery()
		{
			SetupChatService(CreateChatMessage("alt query"));
			var agent = new ResearchAgentFunction(_searchServiceMock.Object, _chatService.Object, _loggerMock.Object);
			var result = await InvokePrivateAsync(agent, "SuggestAlternativeQuery", "original", "topic", "aspect");
			Assert.Equal("alt query", result);
		}

		[Fact]
		public async Task SuggestAlternativeQuery_ReturnsFallback_WhenEmpty()
		{
			SetupChatService(CreateChatMessage(" "));
			var agent = new ResearchAgentFunction(_searchServiceMock.Object, _chatService.Object, _loggerMock.Object);
			var result = await InvokePrivateAsync(agent, "SuggestAlternativeQuery", "original", "topic", "aspect");
			Assert.Equal("topic aspect information", result);
		}

		[Fact]
		public async Task MultiStepResearch_ReturnsNoTopicMessage_WhenTopicEmpty()
		{
			var agent = new ResearchAgentFunction(_searchServiceMock.Object, _chatService.Object, _loggerMock.Object);
			var result = await agent.MultiStepResearch("", new string[] { "aspect" });
			Assert.Equal("No research topic provided.", result);
		}

		private void SetupChatService(params ChatMessageContent[] queue)
		{
			var messageQueue = new Queue<ChatMessageContent>(queue);
			_chatService.SetupSequence(x => x.GetChatMessageContentsAsync(It.IsAny<ChatHistory>(), It.IsAny<PromptExecutionSettings>(), It.IsAny<Kernel>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(() => [messageQueue.Dequeue()]);
		}

		private ChatMessageContent CreateChatMessage(string content)
		{
			return new ChatMessageContent(AuthorRole.Assistant, content);
		}

		private async Task<string> InvokePrivateAsync(object instance, string methodName, params object[] args)
		{
			var type = instance.GetType();
			var method = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (method == null && type.BaseType != null)
			{
				method = type.BaseType.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			}
			if (method == null)
			{
				throw new InvalidOperationException($"Method {methodName} not found.");
			}
			var task = method.Invoke(instance, args) as Task<string>;
			if (task == null)
			{
				throw new InvalidOperationException($"Method {methodName} did not return Task<string>.");
			}

			return await task;
		}
	}
}
