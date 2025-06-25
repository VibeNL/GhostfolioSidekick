
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public class AgentLogger
	{
		public static string CurrentAgentName { get; private set; } = string.Empty;

		internal async Task StartAgent(string name)
		{
			CurrentAgentName = name;
			await Task.Yield();
		}
	}
}