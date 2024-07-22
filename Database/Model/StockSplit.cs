namespace GhostfolioSidekick.Database.Model
{
	public class StockSplit
	{
		public int Id { get; set; }

		public required DateOnly Date { get; set; }

		public int FromAmount { get; }

		public int ToAmount { get; }

		public int SymbolProfileId { get; set; }
	}
}