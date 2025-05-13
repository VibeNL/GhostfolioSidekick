using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	internal class PortfolioSummaryAgent : IAgent
	{
		private IWebChatClient _webChatClient;

		public string Name => nameof(PortfolioSummaryAgent);

		public object Description => "Summarizes and analyzed portfolio performance";

		public PortfolioSummaryAgent(IWebChatClient webChatClient)
		{
			_webChatClient = webChatClient;
		}

		public async IAsyncEnumerable<ChatResponseUpdate> RespondAsync(IEnumerable<ChatMessage> messages, AgentContext context)
		{
			var prompt = $@"
				You are a financial assistant specializing in portfolio analysis.

				Given the following conversation history and user request, provide a concise summary of the user's investment portfolio. 
				Include key performance metrics such as total value, gains/losses, and notable trends. 
				If possible, mention any significant changes or risks in the portfolio. 
				Be clear and use plain language suitable for a non-expert.

				Conversation history:
				{string.Join(Environment.NewLine, context.Memory.Select(m => $"{m.Role}: {m.Text}"))}

				User's latest message:
				""{messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text}""

				Respond with a summary and analysis of the portfolio.
				";

			var llmResponse = await _webChatClient.GetResponseAsync(prompt);

			if (llmResponse.Text == null)
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, "No response from LLM.");
				yield break;
			}

			context.Memory.Add(new ChatMessage(ChatRole.Assistant, llmResponse.Text) { AuthorName = Name });

			// yield the response
			yield return new ChatResponseUpdate(ChatRole.Assistant, llmResponse.Text);
		}
	}
}