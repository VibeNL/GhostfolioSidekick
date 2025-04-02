namespace GhostfolioSidekick.PortfolioViewer.Model
{
	public class Activity
	{
		public int AccountId { get; set; }
		public int? HoldingId { get; set; }
		public DateTime Date { get; set; }
		public required string TransactionId { get; set; }
		public string? Description { get; set; }
		public required string Type { get; set; }
		public long Id { get; set; }
	}
}
