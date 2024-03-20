
namespace GhostfolioSidekick.Parsers
{
	public class HistoricData
	{
		public DateTime Date { get; internal set; }
		public decimal Open { get; internal set; }
		public decimal High { get; internal set; }
		public decimal Low { get; internal set; }
		public decimal Close { get; internal set; }
		public decimal Volume { get; internal set; }
	}
}