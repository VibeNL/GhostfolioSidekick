using Microsoft.SemanticKernel;

namespace GhostfolioSidekick.AI.Common
{
	public static partial class ChatMessageContentHelper
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

			// Collapse multiple spaces into one and trim
			text = WhitespaceRegEx().Replace(text, " ").Trim();

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

		[System.Text.RegularExpressions.GeneratedRegex(@"\s+")]
		private static partial System.Text.RegularExpressions.Regex WhitespaceRegEx();
	}
}
