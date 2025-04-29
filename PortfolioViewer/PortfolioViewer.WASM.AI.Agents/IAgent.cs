using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GhostfolioSidekick.Tools.PortfolioViewer.WASM.AI.Agents
{
	public interface IAgent
	{
		bool CanTerminate { get; }
		string Name { get; }
		bool InitialAgent { get; }
		string Description { get; }

		Task<Agent> Initialize(Kernel kernel);

		Task<bool> PostProcess(ChatHistory history);
	}
}