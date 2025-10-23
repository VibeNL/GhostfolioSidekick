namespace GhostfolioSidekick.AI.Common
{
	public class AgentLogger
	{
		public string CurrentAgentName { get; private set; } = string.Empty;
		public string CurrentAgentFunction { get; private set; } = string.Empty;

		// Event to notify when CurrentAgentName changes
		public event Action? CurrentAgentNameChanged;

		public void StartAgent(string name)
		{
			CurrentAgentName = name;
			CurrentAgentFunction = string.Empty;
			CurrentAgentNameChanged?.Invoke();
		}

		public void StartFunction(string name)
		{
			CurrentAgentFunction = name;
			CurrentAgentNameChanged?.Invoke();
		}
	}
}