using GhostfolioSidekick.AI.Common;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;

namespace GhostfolioSidekick.AI.Agents
{
	public static class GhostfolioSidekick
	{
		private static string BuildPrompt()
		{
			var sb = new StringBuilder();
			sb.AppendLine("You are GhostfolioSidekick AI — a smart financial assistant. Help users understand and manage their investment portfolio.");
			sb.AppendLine("Respond clearly, avoid financial advice disclaimers, and answer in markdown with bullet points or tables when helpful.");
			sb.AppendLine("Use financial terminology and suggest insights like trends or anomalies if data is present.");
			sb.AppendLine($"The current date is {DateTime.UtcNow:yyyy-MM-dd}.");
			sb.AppendLine();
			
			return sb.ToString();
		}

		public static ChatClientAgent Create(ICustomChatClient chatClient, IList<AITool>? tools = null, ChatHistoryProvider? chatHistoryProvider = null)
		{
			var cloned = chatClient.Clone();
			// Default to non-thinking so responses stay short on every device: long thinking streams trip WebGPU
			// buffer-map races (mlc-ai/web-llm#497) on constrained GPUs — phones and low-powered laptops alike.
			cloned.ChatMode = ChatMode.Chat;

			return cloned.AsAIAgent(new ChatClientAgentOptions
			{
				Name = "GhostfolioSidekick",
				Description = "A smart financial assistant that helps users understand and manage their investment portfolio.",
				ChatOptions = new ChatOptions { Instructions = BuildPrompt(), Tools = tools },
				// Falls back to the in-memory provider when null.
				ChatHistoryProvider = chatHistoryProvider,
			});
		}
	}
}
