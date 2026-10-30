using CsvHelper;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Coinbase
{
	[CsvRecord]
	public partial class CoinbaseRecord
	{
		[CsvColumn("ID", Optional = true)]
		public string? TransactionId { get; set; }

		[CsvConverter(typeof(UtcDateTimeConverter))]
		public DateTime Timestamp { get; set; }

		[CsvColumn("Transaction Type")]
		public required string Type { get; set; }

		public required string Asset { get; set; }

		[CsvColumn("Quantity Transacted")]
		public decimal Quantity { get; set; }

		[CsvColumn("Spot Price Currency", "Price Currency")]
		public required string Currency { get; set; }

		[CsvColumn("Spot Price at Transaction", "Price at Transaction")]
		[CsvConverter(typeof(DecimalCurrencyStrippingConverter))]
		public decimal? Price { get; set; }

		[CsvColumn("Total (inclusive of fees and/or spread)")]
		[CsvConverter(typeof(DecimalCurrencyStrippingConverter))]
		public decimal? TotalTransactionAmount { get; set; }

		[CsvColumn("Fees and/or Spread")]
		[CsvConverter(typeof(DecimalCurrencyStrippingConverter))]
		public decimal? Fee { get; set; }

		public required string Notes { get; set; }

		// Derived fields that are only positive
		public decimal PositiveQuantity => Math.Abs(Quantity);

		public decimal? PositiveTotalTransactionAmount => Math.Abs(TotalTransactionAmount ?? 0);
	}
}
