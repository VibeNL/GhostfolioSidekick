using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.GhostfolioAPI.Contract
{
	[ExcludeFromCodeCoverage]
	public class MarketData
	{
		public DateTime Date { get; set; }

		public required string Symbol { get; set; }

		public decimal MarketPrice { get; set; }

		public required string DataSource { get; set; }

		public int ActivitiesCount { get; set; }
	}
}