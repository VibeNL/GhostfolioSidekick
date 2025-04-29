using System.Text;
using System.Threading.Tasks;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GhostfolioSidekick.Tools.PortfolioViewer.WASM.AI.Agents
{
	public class FinancialAgent(string name, ILogger<FinancialAgent> logger) : IAgent
	{
		public bool CanTerminate => true;

		public string Name => nameof(FinancialAgent);

		public bool InitialAgent => true;

		public string Description => "The financial expert who can summarize and explain results, and delegate data questions.";

		public async Task<Agent> Initialize(Kernel kernel)
		{
			var chatCompletionAgent = new ChatCompletionAgent
			{
				Instructions = $"""
								You are a financial expert with deep knowledge in markets, personal finance, and investments.

				Behavior rules:
				- You NEVER attempt to generate SQL.
				- If a user question requires portfolio-specific or market data from the database, you MUST delegate to the agent "{nameof(GenericQueryAgent)}" by replying with only:
				  {nameof(GenericQueryAgent)}

				- Once you receive a result from {nameof(GenericQueryAgent)}, read the results and summarize the answer clearly in natural language.
				- Use concise explanations with no repetition or SQL code.
				- Do NOT include markdown formatting like code blocks or backticks.
				- Do NOT include your thought process or justification unless explicitly asked.
				- If you are done, you can say "Done" or "Finished" to indicate completion.
				- Do not hallucinate or make up information. If you don't know the answer, say "I don't know" or "I can't help with that."
				""",
				Name = name,
				Kernel = kernel
			};

			return chatCompletionAgent;
		}

		public Task<bool> PostProcess(ChatHistory history)
		{
			return Task.FromResult(false);
		}
	}
}
