using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public static class GhostfolioSidekick
	{
		private static string BuildPromptWithCompanions(IEnumerable<Agent> companions)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"The Currentdate is {DateTime.UtcNow:yyyy-MM-dd}.");
			sb.AppendLine("You are GhostfolioSidekick AI — a smart financial assistant. Help users understand and manage their investment portfolio.");
			sb.AppendLine("Respond clearly, avoid financial advice disclaimers, and answer in markdown with bullet points or tables when helpful.");
			sb.AppendLine("Use financial terminology and suggest insights like trends or anomalies if data is present.");
			sb.AppendLine();
			sb.AppendLine("You have access to a set of specialized companions (other agents) for specific tasks. When calling one, only repond with the name of the agent and the query to that agent");
			sb.AppendLine("If a user request is better handled by a companion, only call or refer to the appropriate companion agent by name at the moment of delegation.");
			sb.AppendLine("Clearly indicate when you are delegating a task to a companion agent and do not provide any other data when doing so.");
			sb.AppendLine();
			sb.AppendLine("Available companions:");
			foreach (var companion in companions)
			{
				sb.AppendLine($"- {companion.Name}: {companion.Description}");
			}
			return sb.ToString();
		}

		public static ChatCompletionAgent Create(IWebChatClient webChatClient, IEnumerable<Agent> companions)
		{
			IKernelBuilder thinkBuilder = Kernel.CreateBuilder();
			thinkBuilder.Services.AddScoped<IChatCompletionService>((s) =>
			{
				var client = webChatClient.Clone();
				client.ChatMode = ChatMode.ChatWithThinking;
				return client.AsChatCompletionService();
			});
			var thinkingKernel = thinkBuilder.Build();

			return new ChatCompletionAgent
			{
				Name = "GhostfolioSidekick",
				Instructions = BuildPromptWithCompanions(companions),
				Kernel = thinkingKernel,
				Description = "A smart financial assistant that helps users understand and manage their investment portfolio.",
				InstructionsRole = AuthorRole.System
			};
		}
	}
}