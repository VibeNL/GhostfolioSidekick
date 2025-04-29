using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GhostfolioSidekick.Tools.PortfolioViewer.WASM.AI.Agents
{
	public class CriticAgent(string name, ILogger<CriticAgent> logger) : IAgent
	{
		public bool CanTerminate => false;

		public string Name => nameof(CriticAgent);

		public bool InitialAgent => false;

		public string Description => "Reviews and critiques other agent responses";

		public async Task<Agent> Initialize(Kernel kernel)
		{
			var chatCompletionAgent = new ChatCompletionAgent
			{
				Instructions = $"""
								Reviews and critiques other agent responses.
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
