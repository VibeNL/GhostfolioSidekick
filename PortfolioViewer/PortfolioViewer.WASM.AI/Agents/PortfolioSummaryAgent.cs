using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	internal class PortfolioSummaryAgent : IAgent
	{
		private IWebChatClient _webChatClient;

		public string Name => nameof(PortfolioSummaryAgent);

		public PortfolioSummaryAgent(IWebChatClient webChatClient)
		{
			_webChatClient = webChatClient;
		}

		public async IAsyncEnumerable<ChatResponseUpdate> RespondAsync(IEnumerable<ChatMessage> messages, AgentContext context)
		{
			// add a dummy response
			var response = new ChatResponseUpdate(ChatRole.Assistant, "This is a dummy response from the PortfolioSummaryAgent.");
			// add the response to the context memory
			context.Memory.Add(new ChatMessage(ChatRole.Assistant, response.Text) { AuthorName = Name });
			// yield the response
			yield return response;
		}
	}
}