
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI
{
	public class AgentLogger
	{
		private static readonly Lock @lock = new();
		public static string CurrentAgentName { get; private set; } = string.Empty;

		internal void StartAgent(string name)
		{
			// Ensure thread safety when accessing the static property
			lock (@lock)
			{
				CurrentAgentName = name;
			}
		}
	}
}