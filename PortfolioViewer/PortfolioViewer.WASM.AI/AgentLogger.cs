using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public class AgentLogger
	{
		public static string CurrentAgentName { get; private set; } = string.Empty;

		// Event to notify when CurrentAgentName changes
		public static event Action? CurrentAgentNameChanged;

		internal async Task StartAgent(string name)
		{
			CurrentAgentName = name;
			CurrentAgentNameChanged?.Invoke();
			//await Task.Yield();
		}
	}
}