namespace PortfolioViewer.WASM.AI.Agents
{
	public interface IAgent
	{
		Task<string> HandleAsync(string task, AgentContext context);
	}
}
