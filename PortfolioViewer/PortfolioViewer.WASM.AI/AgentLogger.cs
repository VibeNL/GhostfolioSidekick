namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public class AgentLogger
	{
		public string CurrentAgentName { get; private set; } = string.Empty;
		public string CurrentAgentFunction { get; private set; } = string.Empty;

		// Event to notify when CurrentAgentName changes
		public event Action? CurrentAgentNameChanged;

		internal async Task StartAgent(string name)
		{
			CurrentAgentName = name;
			CurrentAgentFunction = string.Empty;
			CurrentAgentNameChanged?.Invoke();
		}

		internal async Task StartFunction(string name)
		{
			CurrentAgentFunction = name;
			CurrentAgentNameChanged?.Invoke();
		}
	}
}