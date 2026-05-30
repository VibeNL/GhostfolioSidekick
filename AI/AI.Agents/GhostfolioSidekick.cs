using GhostfolioSidekick.AI.Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;

namespace GhostfolioSidekick.AI.Agents
{
	public static class GhostfolioSidekick
	{
		private static string BuildPrompt(IEnumerable<(string Name, string Description)> companions)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"The Currentdate is {DateTime.UtcNow:yyyy-MM-dd}.");
			sb.AppendLine("You are GhostfolioSidekick AI — a smart financial assistant. Help users understand and manage their investment portfolio.");
			sb.AppendLine("Respond clearly, avoid financial advice disclaimers, and answer in markdown with bullet points or tables when helpful.");
			sb.AppendLine("Use financial terminology and suggest insights like trends or anomalies if data is present.");
			sb.AppendLine();
			sb.AppendLine("You have access to a set of specialized tools for specific tasks.");
			sb.AppendLine("If a user request is better handled by a tool, call the appropriate tool.");
			sb.AppendLine();
			sb.AppendLine("Available tools:");
			foreach (var (name, description) in companions)
			{
				sb.AppendLine($"- {name}: {description}");
			}
			return sb.ToString();
		}

		public static ChatClientAgent Create(ICustomChatClient chatClient, IEnumerable<(string Name, string Description)> companions, IList<AITool>? tools = null)
		{
			var cloned = chatClient.Clone();
			cloned.ChatMode = ChatMode.ChatWithThinking;

			return cloned.AsAIAgent(
				instructions: BuildPrompt(companions),
				name: "GhostfolioSidekick",
				description: "A smart financial assistant that helps users understand and manage their investment portfolio.",
				tools: tools);
		}
	}
}
