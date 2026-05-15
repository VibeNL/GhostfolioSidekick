namespace GhostfolioSidekick.PortfolioViewer.WASM.Pages
{
	public class MigrationStatusViewModel
	{
		public List<string> AppliedMigrations { get; set; } = [];
		public List<string> PendingMigrations { get; set; } = [];
		public bool IsUpToDate => PendingMigrations.Count == 0;
	}
}