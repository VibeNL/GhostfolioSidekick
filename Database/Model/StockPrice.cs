namespace GhostfolioSidekick.Database.Model
{
	public class StockPrice
	{
		public DateOnly Date { get; set; }

		public double Open { get; set; }

		public double High { get; set; }

		public double Low { get; set; }

		public double Close { get; set; }

		public double AdjClose { get; set; }

		public double Volume { get; set; }

		public required Currency Currency { get; set; }
	}
}