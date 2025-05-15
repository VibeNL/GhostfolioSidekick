using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public static class ChatMessageHelper
	{
		public static string ToDisplayText(this ChatMessage message)
		{
			if (message == null)
			{
				return string.Empty;
			}

			var text = System.Text.RegularExpressions.Regex.Replace(
				message.Text,
				@"<think>.*?</think>",
				string.Empty,
				System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
			, TimeSpan.FromMinutes(1));

			return text;
		}

		public static string ToThinkText(this ChatMessage message)
		{
			if (message == null)
			{
				return string.Empty;
			}

			var match = System.Text.RegularExpressions.Regex.Match(
				message.Text,
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
