using GhostfolioSidekick.PortfolioAnalysis;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Tasks
{
	/// <summary>
	/// Background task to maintain portfolio performance storage (cleanup old versions)
	/// </summary>
	public class Performance
	{
		public TaskPriority Priority => TaskPriority.StorageMaintenance;
	}
}