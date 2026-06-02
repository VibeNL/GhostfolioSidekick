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

			return WhitespaceRegEx().Replace(message, " ").Trim();
		}

		public static string ToThinkText(this ChatMessage message)
		{
			if (message == null)
			{
				return string.Empty;
			}

			var text = message.AdditionalProperties?["tool_call"]?.ToString();

			return ToDisplayText(text ?? string.Empty);

		}

		[System.Text.RegularExpressions.GeneratedRegex(@"\s+")]
		private static partial System.Text.RegularExpressions.Regex WhitespaceRegEx();
	}
}
