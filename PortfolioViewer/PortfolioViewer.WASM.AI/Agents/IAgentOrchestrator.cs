
namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.Agents
{
	public interface IAgentOrchestrator
	{
		Task<string> GetResponseAsync(string input, AgentContext context);
	}
}