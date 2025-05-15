using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public class SummaryAgent(IWebChatClient webChatClient) : IAgent
	{
		public string Name => nameof(SummaryAgent);

		public string Description => "Provides concise summaries of chat conversations or portfolio data.";

		public bool IsDefault => false;

		public async IAsyncEnumerable<ChatResponseUpdate> RespondAsync(IEnumerable<ChatMessage> messages, AgentContext context)
		{
			var prompt = $@"
				You are a helpful assistant. Summarize the conversation above in a clear and concise manner, 
				highlighting the main topics, questions, and any important conclusions or action items. 
				Use plain language suitable for a general audience.

				Conversation history:
				{string.Join(Environment.NewLine, context.Memory.Select(m => $"{m.Role}: {m.Text}"))}

				User's latest message:
				""{messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text}""
				";

			var llmResponse = await webChatClient.GetResponseAsync(prompt);

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
