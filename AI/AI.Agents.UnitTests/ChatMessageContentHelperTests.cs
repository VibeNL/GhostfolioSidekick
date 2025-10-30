using GhostfolioSidekick.AI.Common;
using Microsoft.SemanticKernel;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class ChatMessageContentHelperTests
	{
		[Fact]
		public void ToDisplayText_String_RemovesThinkTagsAndReturnsText()
		{
			var input = "Hello <think>internal</think> world!";
			var result = ChatMessageContentHelper.ToDisplayText(input);
			Assert.Equal("Hello world!", result);
		}

		[Fact]
		public void ToDisplayText_String_Null_ReturnsEmptyString()
		{
			string? input = null;
			var result = ChatMessageContentHelper.ToDisplayText(input);
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ToDisplayText_ChatMessageContent_RemovesThinkTagsAndReturnsText()
		{
			var message = new ChatMessageContent { Content = "Hi <think>internal</think> there!" };
			var result = ChatMessageContentHelper.ToDisplayText(message);
			Assert.Equal("Hi there!", result);
		}

		[Fact]
		public void ToDisplayText_ChatMessageContent_Null_ReturnsEmptyString()
		{
			ChatMessageContent? message = null;
			var result = ChatMessageContentHelper.ToDisplayText(message!);
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ToThinkText_ChatMessageContent_ReturnsThinkText()
		{
			var message = new ChatMessageContent { Content = "Visible <think>hidden</think> text" };
			var result = ChatMessageContentHelper.ToThinkText(message);
			Assert.Equal("hidden", result);
		}

		[Fact]
		public void ToThinkText_ChatMessageContent_NoThinkTag_ReturnsEmptyString()
		{
			var message = new ChatMessageContent { Content = "Visible text only" };
			var result = ChatMessageContentHelper.ToThinkText(message);
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ToThinkText_ChatMessageContent_Null_ReturnsEmptyString()
		{
			ChatMessageContent? message = null;
			var result = ChatMessageContentHelper.ToThinkText(message!);
			Assert.Equal(string.Empty, result);
		}
	}
}
