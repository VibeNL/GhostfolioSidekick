using Moq;
using GhostfolioSidekick.AI.Functions.OnlineSearch;
using GhostfolioSidekick.AI.Common;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;

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

		private class TestChatCompletionService : IChatCompletionService
		{
			public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

			public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
			{
				return Task.FromResult<IReadOnlyList<ChatMessageContent>>(
					new List<ChatMessageContent> { new(AuthorRole.Assistant, "summary") }
				);
			}

			public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory chatHistory, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
			{
				return GetEmptyAsyncEnumerable();
			}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
			private static async IAsyncEnumerable<StreamingChatMessageContent> GetEmptyAsyncEnumerable()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
			{
				yield break;
			}
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


			var chatService = new TestChatCompletionService();
			var function = new ResearchAgentFunction(searchServiceMock.Object, chatService, modelInfo, loggerMock.Object);
			var topic = "TestTopic";
			var aspects = new[] { "Aspect1", "Aspect2" };

			// Act
			var result = await function.MultiStepResearch(topic, aspects);

			// Assert
			Assert.Contains("summary", result);
		}
	}
}
