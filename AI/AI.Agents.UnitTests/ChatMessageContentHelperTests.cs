using GhostfolioSidekick.AI.Common;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Agents.UnitTests
{
	public class ChatMessageContentHelperTests
	{
		[Fact]
		public void ToDisplayText_String_Null_ReturnsEmptyString()
		{
			string? input = null;
			var result = ChatMessageContentHelper.ToDisplayText(input);
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ToDisplayText_ChatMessage_Null_ReturnsEmptyString()
		{
			ChatMessage? message = null;
			var result = ChatMessageContentHelper.ToDisplayText(message!);
			Assert.Equal(string.Empty, result);
		}

		[Fact]
		public void ToThinkText_ChatMessage_NoThinkTag_ReturnsEmptyString()
		{
			var message = new ChatMessage(ChatRole.Assistant, "Visible text only");
			var result = ChatMessageContentHelper.ToThinkText(message);
			Assert.Equal(string.Empty, result);
		}
	}
}
