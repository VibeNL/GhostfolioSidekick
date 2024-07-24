namespace GhostfolioSidekick.Database.Model
{
	public class StockSplit
	{
		public int Id { get; set; }

		public required DateOnly Date { get; set; }

		public int FromAmount { get; set; }

		public int ToAmount { get; set;  }

		public int StockSplitListId { get; set; }

		public StockSplitList StockSplitList { get; set; }
	}
}