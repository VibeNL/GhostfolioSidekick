using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Coinbase
{
	[Delimiter(",")]
	public class MacroTrendsRecord
	{
		[DateTimeStyles(DateTimeStyles.AssumeUniversal)]
		[Format("yyyy-MM-dd")]
		public DateTime Date { get; set; }

		public decimal Open { get; set; }

		public decimal High { get; set; }

		public decimal Low { get; set; }

		public decimal Close { get; set; }

		public decimal Volume { get; set; }
	}
}