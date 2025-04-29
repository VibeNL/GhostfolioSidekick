using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel;

namespace GhostfolioSidekick.Tools.PortfolioViewer.WASM.AI.Agents
{
	public interface IAgent
	{
		bool CanTerminate { get; }
		string Name { get; }
		bool InitialAgent { get; }
		object? Description { get; }

		public Task<Agent> Initialize(Kernel kernel);
	}
}