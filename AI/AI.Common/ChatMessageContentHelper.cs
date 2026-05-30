using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.AI.Common
{
	public static partial class ChatMessageContentHelper
	{
		public static string ToDisplayText(this ChatMessage message)
		{
			return ToDisplayText(message?.Text) ?? string.Empty;
		}

		public static string ToDisplayText(this string? message)
		{
			if (message == null)
			{
				return string.Empty;
			}

			var text = ThinkTagRegEx().Replace(message, string.Empty);
			return WhitespaceRegEx().Replace(text, " ").Trim();
		}

		public static string ToThinkText(this ChatMessage message)
		{
			if (message == null)
			{
				return string.Empty;
			}

			var match = ThinkTagCaptureRegEx().Match(message.Text ?? string.Empty);

			if (!match.Success)
			{
				return string.Empty;
			}

			return match.Groups[1].Value;
		}

		[System.Text.RegularExpressions.GeneratedRegex(@"<think>.*?</think>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
		private static partial System.Text.RegularExpressions.Regex ThinkTagRegEx();

		[System.Text.RegularExpressions.GeneratedRegex(@"<think>(.*?)</think>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase)]
		private static partial System.Text.RegularExpressions.Regex ThinkTagCaptureRegEx();

		[System.Text.RegularExpressions.GeneratedRegex(@"\s+")]
		private static partial System.Text.RegularExpressions.Regex WhitespaceRegEx();
	}
}
