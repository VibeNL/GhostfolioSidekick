namespace GhostfolioSidekick.PortfolioViewer.Model
{
	public record Money
	{
		public decimal Amount { get; set; }

		public Currency Currency { get; set; }
	}
}