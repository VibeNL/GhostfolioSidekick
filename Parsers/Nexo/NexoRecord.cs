using CsvHelper;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Nexo
{
	[CsvRecord]
	public partial class NexoRecord
	{
		public required string Transaction { get; set; }

		public required string Type { get; set; }

		[CsvColumn("Input Currency")]
		public required string InputCurrency { get; set; }

		[CsvColumn("Input Amount")]
		public decimal InputAmount { get; set; }

		[CsvColumn("Output Currency")]
		public required string OutputCurrency { get; set; }

		[CsvColumn("Output Amount")]
		public decimal OutputAmount { get; set; }

		[CsvColumn("USD Equivalent")]
		[CsvConverter(typeof(EnUsCurrencyConverter))]
		public decimal USDEquivalent { get; set; }

		[CsvColumn("Details")]
		public required string Details { get; set; }

		[CsvColumn("Date / Time (UTC)")]
		[CsvConverter(typeof(UtcDateTimeConverter))]
		public DateTime DateTime { get; set; }
	}
}
