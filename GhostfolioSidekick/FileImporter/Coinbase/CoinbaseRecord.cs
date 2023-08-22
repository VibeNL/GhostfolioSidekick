using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.FileImporter.Coinbase
{
	[Delimiter(",")]
	public class CoinbaseRecord
	{
		// Timestamp,Transaction Type,Asset,Quantity Transacted,Spot Price Currency,Spot Price at Transaction,Subtotal,Total (inclusive of fees and/or spread),Fees and/or Spread,Notes

		[Format("yyyy-MM-ddTHH:mm:ssZ")]
		public DateOnly Timestamp { get; set; }

		[Name("Transaction Type")]
		public string Order { get; set; }

		public string Asset { get; set; }

		[Name("Quantity Transacted")]
		public decimal Quantity { get; set; }

		[Name("Spot Price Currency")]
		public decimal Price { get; set; }

		[Name("Spot Price at Transaction")]
		public decimal UnitPrice { get; set; }

		public decimal Subtotal { get; set; }

		[Name("Total (inclusive of fees and/or spread)")]
		public decimal Total { get; set; }

		[Name("Fees and/or Spread")]
		public decimal Fee{ get; set; }

		public string Notes { get; set; }
	}
}
