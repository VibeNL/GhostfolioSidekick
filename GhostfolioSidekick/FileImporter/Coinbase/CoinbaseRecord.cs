using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Coinbase
{
	[Delimiter(",")]
	public class CoinbaseRecord
	{
		// Timestamp,Transaction Type,Asset,Quantity Transacted,Spot Price Currency,Spot Price at Transaction,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes

		[Format("yyyy-MM-ddTHH:mm:ssZ")]
		public DateTime Timestamp { get; set; }

		[Name("Transaction Type")]
		public string Order { get; set; }

		public string Asset { get; set; }

		[Name("Quantity Transacted")]
		[NumberStyles(NumberStyles.Number | NumberStyles.AllowExponent)]
		public decimal Quantity { get; set; }

		[Name("Spot Price Currency")]
		public string Currency { get; set; }

		[Name("Spot Price at Transaction")]
		public decimal? UnitPrice { get; set; }

		public decimal? Subtotal { get; set; }

		[Name("Total (inclusive of fees and/or spread)")]
		public decimal? Total { get; set; }

		[Name("Fees and/or Spread")]
		public decimal? Fee{ get; set; }

		public string Notes { get; set; }
	}
}
