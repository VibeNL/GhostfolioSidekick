using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Nexo
{
	[Delimiter(",")]
	public class NexoRecord
	{
		public required string Transaction { get; set; }

		public required string Type { get; set; }

		[Name("Input Currency")]
		public required string InputCurrency { get; set; }

		[Name("Input Amount")]
		public decimal InputAmount { get; set; }

		[Name("Output Currency")]
		public required string OutputCurrency { get; set; }

		[Name("Output Amount")]
		public decimal OutputAmount { get; set; }

		[Name("USD Equivalent")]
		public required string USDEquivalent { get; set; }

		[Name("Details")]
		public required string Details { get; set; }

		[DateTimeStyles(DateTimeStyles.AssumeUniversal)]
		[Name("Date / Time (UTC)")]
		[Format("yyyy-MM-dd HH:mm:ss")]
		public DateTime DateTime { get; set; }
	}
}