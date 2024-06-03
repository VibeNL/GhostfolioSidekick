
namespace GhostfolioSidekick.Parsers
{
	public class HistoricData
	{
		public DateTime Date { get; set; }

		public decimal Open { get; set; }

		public decimal High { get; set; }

		public decimal Low { get; set; }

		public decimal Close { get; set; }

		public decimal Volume { get; set; }

		public required string Symbol { get; set; }
	}
}