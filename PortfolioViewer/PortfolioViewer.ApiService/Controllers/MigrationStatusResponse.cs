namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	public class MigrationStatusResponse
	{
		public List<string> AppliedMigrations { get; set; } = [];
		public List<string> PendingMigrations { get; set; } = [];
		public bool IsUpToDate => PendingMigrations.Count == 0;
	}
}