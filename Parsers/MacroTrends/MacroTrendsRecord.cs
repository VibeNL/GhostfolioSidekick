using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Coinbase
{
	[Delimiter(",")]
	public class MacroTrendsRecord
	{
		[DateTimeStyles(DateTimeStyles.AssumeUniversal)]
		[Format("yyyy-MM-dd")]
		[Name("date")]
		public DateTime Date { get; set; }

		[Name("open")]
		public decimal Open { get; set; }

		[Name("high")]
		public decimal High { get; set; }

		[Name("low")]
		public decimal Low { get; set; }

		[Name("close")]
		public decimal Close { get; set; }

		[Name("volume")]
		public decimal Volume { get; set; }
	}
}