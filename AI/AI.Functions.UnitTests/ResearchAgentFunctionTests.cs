using Moq;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using GhostfolioSidekick.AI.Common;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Functions.UnitTests
{
	public class ResearchAgentFunctionTests
	{
		[Theory]
		[InlineData("<b>Test</b>", "Test")]
		[InlineData("Hello &amp; World", "Hello & World")]
		[InlineData("<div>Hi &lt;there&gt;</div>", "Hi <there>")]
		[InlineData("", "")]
		public void SanitizeText_RemovesHtmlAndDecodesEntities(string input, string expected)
		{
			var result = ResearchAgentFunction.SanitizeText(input);
			Assert.Equal(expected, result);
		}

		[Theory]
		[InlineData("short", "short")]
		[InlineData("", "")]
		public void TruncatePrompt_TruncatesLongPrompt(string input, string expected)
		{
			var result = ResearchAgentFunction.TruncatePrompt(input, 100);
			Assert.Equal(expected, result);
		}

		private class TestChatClient : IChatClient
		{
			public static ChatClientMetadata Metadata => new("test", null, null);

			public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
			{
				var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "summary")]);
				return Task.FromResult(response);
			}

			public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
			{
				return AsyncEnumerable.Empty<ChatResponseUpdate>();
			}

			public object? GetService(Type serviceType, object? serviceKey = null) => null;
			public void Dispose() { }
		}

		private class RecordingChatClient : IChatClient
		{
			public List<string> Prompts { get; } = [];

			public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
			{
				Prompts.Add(string.Join("\n", messages.Select(m => m.Text)));
				return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, "summary")]));
			}

			public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
			{
				throw new NotImplementedException();
			}

			public object? GetService(Type serviceType, object? serviceKey = null) => null;
			public void Dispose() { }
		}

		[Fact]
		public async Task MultiStepResearch_ReturnsSynthesizedSummary()
		{
			// Arrange
			var searchServiceMock = new Mock<IGoogleSearchService>();
			var loggerMock = new Mock<AgentLogger>();

			var searchResults = new List<WebResult>
			{
				new() { Content = "<b>Result1</b> &amp; info" },
				new() { Content = "<i>Result2</i>" },
				new() { Content = "Result3" }
			};
			searchServiceMock.Setup(s => s.SearchAsync(It.IsAny<string>()))
				.ReturnsAsync(searchResults);
			var modelInfo = new ModelInfo { Name = "test-model", MaxTokens = 1000 };

			var chatService = new TestChatClient();
			var function = new ResearchAgentFunction(searchServiceMock.Object, chatService, modelInfo, loggerMock.Object);
			var topic = "TestTopic";
			var aspects = new[] { "Aspect1", "Aspect2" };

			// Act
			var result = await function.MultiStepResearch(topic, aspects, TestContext.Current.CancellationToken);

			// Assert
			Assert.Contains("summary", result);
		}

		[Fact]
		public async Task MultiStepResearch_WithEmptyTopic_ReturnsGuidanceWithoutSearching()
		{
			// Arrange
			var searchServiceMock = new Mock<IGoogleSearchService>();
			var loggerMock = new Mock<AgentLogger>();
			var modelInfo = new ModelInfo { Name = "test-model", MaxTokens = 1000 };
			var chatService = new TestChatClient();
			var function = new ResearchAgentFunction(searchServiceMock.Object, chatService, modelInfo, loggerMock.Object);

			// Act
			var result = await function.MultiStepResearch("   ", ["Aspect1"], TestContext.Current.CancellationToken);

			// Assert
			Assert.Contains("topic", result, StringComparison.OrdinalIgnoreCase);
			searchServiceMock.Verify(s => s.SearchAsync(It.IsAny<string>()), Times.Never);
		}

		[Fact]
		public async Task MultiStepResearch_WithNoAspects_ReturnsGuidanceWithoutSearching()
		{
			// Arrange
			var searchServiceMock = new Mock<IGoogleSearchService>();
			var loggerMock = new Mock<AgentLogger>();
			var modelInfo = new ModelInfo { Name = "test-model", MaxTokens = 1000 };
			var chatService = new TestChatClient();
			var function = new ResearchAgentFunction(searchServiceMock.Object, chatService, modelInfo, loggerMock.Object);

			// Act
			var result = await function.MultiStepResearch("TestTopic", [], TestContext.Current.CancellationToken);

			// Assert
			Assert.Contains("aspect", result, StringComparison.OrdinalIgnoreCase);
			searchServiceMock.Verify(s => s.SearchAsync(It.IsAny<string>()), Times.Never);
		}

		[Fact]
		public async Task MultiStepResearch_WithNoSearchResults_IncludesNoDataNote()
		{
			// Arrange
			var searchServiceMock = new Mock<IGoogleSearchService>();
			searchServiceMock.Setup(s => s.SearchAsync(It.IsAny<string>()))
				.ReturnsAsync(new List<WebResult>());
			var loggerMock = new Mock<AgentLogger>();
			var modelInfo = new ModelInfo { Name = "test-model", MaxTokens = 1000 };

			var chatService = new RecordingChatClient();
			var function = new ResearchAgentFunction(searchServiceMock.Object, chatService, modelInfo, loggerMock.Object);

			// Act
			await function.MultiStepResearch("TestTopic", ["Aspect1"], TestContext.Current.CancellationToken);

			// Assert: the aspect synthesis prompt must tell the model that no data was found.
			Assert.Contains(chatService.Prompts, p => p.Contains("No web search results were found"));
		}

		[Fact]
		public void TruncatePrompt_DoesNotSplitSurrogatePair()
		{
			// Arrange: 10 ASCII chars + one emoji (a UTF-16 surrogate pair) = length 12.
			var prompt = new string('a', 10) + "\U0001F600";

			// Act: cutting at 11 would land inside the surrogate pair.
			var result = ResearchAgentFunction.TruncatePrompt(prompt, 11);

			// Assert
			Assert.Equal(new string('a', 10), result);
		}

		[Fact]
		public void TruncatePrompt_WithZeroMaxTokens_ReturnsEmpty()
		{
			// Act
			var result = ResearchAgentFunction.TruncatePrompt("some prompt", 0);

			// Assert
			Assert.Equal(string.Empty, result);
		}
	}
}
