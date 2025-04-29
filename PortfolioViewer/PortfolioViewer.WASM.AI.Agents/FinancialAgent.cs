using System.Text;
using System.Threading.Tasks;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace GhostfolioSidekick.Tools.PortfolioViewer.WASM.AI.Agents
{
	public class FinancialAgent(string name, ILogger<FinancialAgent> logger) : IAgent
	{
		public bool CanTerminate => true;

		public string Name => nameof(FinancialAgent);

		public bool InitialAgent => true;

		public object? Description => "the financial expert";

		public async Task<Agent> Initialize(Kernel kernel)
		{
			var chatCompletionAgent = new ChatCompletionAgent
			{
				Instructions = $"""
								You are a financial expert specializing in stocks, bonds, and other financial instruments. 
								You provide accurate, concise answers about financial markets, investment strategies, and personal finance.

								If a question involves portfolio-specific data that you cannot answer directly, delegate the query to 
								the agent named {nameof(GenericQueryAgent)} and wait for its response before continuing.
								You must respond with only the exact name of the next agent to speak (e.g., {nameof(GenericQueryAgent)}), and nothing else. Do not include explanations, advice, or formatting.
								
								Only include the relevant answer or query—do not include any explanations, reasoning steps, or additional commentary.
								""",
				Name = name,
				Kernel = kernel
			};

			return chatCompletionAgent;
		}
	}
}
