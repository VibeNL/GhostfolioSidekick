using Microsoft.SemanticKernel;

namespace GhostfolioSidekick.AI.Common
{
	public static class ChatMessageContentHelper
	{
		public static string ToDisplayText(this ChatMessageContent message)
		{
			return ToDisplayText(message?.Content) ?? string.Empty;
		}

		public static string ToDisplayText(this string? message)
		{
			if (message == null)
			{
				return string.Empty;
			}

			var text = System.Text.RegularExpressions.Regex.Replace(
				message,
				@"<think>.*?</think>",
				string.Empty,
				System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
			, TimeSpan.FromMinutes(1));

			return text;
		}

		public static string ToThinkText(this ChatMessageContent message)
		{
			if (message == null)
			{
				return string.Empty;
			}

			var match = System.Text.RegularExpressions.Regex.Match(
				message.Content ?? string.Empty,
				@"<think>(.*?)</think>",
				System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase,
				TimeSpan.FromMinutes(1));

			if (!match.Success)
			{
				return string.Empty;
			}

			return match.Groups[1].Value;
		}

	}
}
