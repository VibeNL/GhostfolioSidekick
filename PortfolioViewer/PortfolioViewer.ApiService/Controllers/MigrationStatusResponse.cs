using System.Collections.Generic;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	public class MigrationStatusResponse
	{
		public List<string> AppliedMigrations { get; set; } = new();
		public List<string> PendingMigrations { get; set; } = new();
		public bool IsUpToDate => PendingMigrations.Count == 0;
	}
}