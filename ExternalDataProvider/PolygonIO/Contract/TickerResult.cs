using System.Globalization;
using System.Text.Json.Serialization;

namespace GhostfolioSidekick.ExternalDataProvider.PolygonIO.Contract
{
	public class TickerResult
	{
		// Trading volume
		public decimal V { get; set; }

		// Volume weighted average price
		public decimal VW { get; set; }

		// Open
		public decimal O { get; set; }

		// Close
		public decimal C { get; set; }

		// High
		public decimal H { get; set; }

		// Low
		public decimal L { get; set; }

		// Unix timestamp
		public long T { get; set; }

		// Number of transactions
		public decimal N { get; set; }
	}
}