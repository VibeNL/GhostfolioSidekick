namespace GhostfolioSidekick.Database.Model
{
	public class StockSplit
	{
		public required DateOnly Date { get; set; }

		public int FromAmount { get; }

		public int ToAmount { get; }
	}
}