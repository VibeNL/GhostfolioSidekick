using GhostfolioSidekick.AI.Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;

namespace GhostfolioSidekick.AI.Agents
{
	public static class GhostfolioSidekick
	{
		private static string BuildPrompt(string? capabilities)
		{
			var sb = new StringBuilder();
			sb.AppendLine("You are GhostfolioSidekick AI — a smart financial assistant. Help users understand and manage their investment portfolio.");
			sb.AppendLine("Respond clearly, avoid financial advice disclaimers, and answer in markdown with bullet points or tables when helpful.");
			sb.AppendLine("Use financial terminology and suggest insights like trends or anomalies if data is present.");
			sb.AppendLine($"The current date is {DateTime.UtcNow:yyyy-MM-dd}.");

			if (!string.IsNullOrWhiteSpace(capabilities))
			{
				sb.AppendLine();
				sb.AppendLine("Available tool groups you can use:");
				sb.Append(capabilities);
				sb.AppendLine();
			}

			return sb.ToString();
		}

		public static ChatClientAgent Create(ICustomChatClient chatClient, IList<AITool>? tools = null, string? capabilities = null)
		{
			var cloned = chatClient.Clone();
			cloned.ChatMode = ChatMode.ChatWithThinking;

			return cloned.AsAIAgent(
				instructions: BuildPrompt(capabilities),
				name: "GhostfolioSidekick",
				description: "A smart financial assistant that helps users understand and manage their investment portfolio.",
				tools: tools);
		}
	}
}
