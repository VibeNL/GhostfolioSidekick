using System.Collections.Generic;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public class MigrationStatusViewModel
	{
		public List<string> AppliedMigrations { get; set; } = new();
		public List<string> PendingMigrations { get; set; } = new();
		public bool IsUpToDate => PendingMigrations.Count == 0;
	}
}