using CsvHelper.Configuration.Attributes;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Nexo
{
	[Delimiter(",")]
	public class NexoRecord
	{
		public string Transaction { get; set; }

		public string Type { get; set; }

		[Name("Input Currency")]
		public string InputCurrency { get; set; }

		[Name("Input Amount")]
		public decimal InputAmount { get; set; }

		[Name("Output Currency")]
		public string OutputCurrency { get; set; }

		[Name("Output Amount")]
		public decimal OutputAmount { get; set; }

		[Name("USD Equivalent")]
		public string USDEquivalent { get; set; }

		[Name("Details")]
		public string Details { get; set; }

		[Name("Date / Time")]
		[Format("yyyy-MM-dd HH:mm:ss")]
		public DateTime DateTime { get; set; }

		public decimal GetUSDEquivalent()
		{
			return decimal.Parse(USDEquivalent, NumberStyles.Currency, new NumberFormatInfo { CurrencyDecimalSeparator = ".", CurrencySymbol = "$" });
		}
	}
}