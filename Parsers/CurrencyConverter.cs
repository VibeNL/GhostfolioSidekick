using CsvHelper;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GhostfolioSidekick.Parsers
{
	public abstract class CurrencyConverter(string culture) : ICsvConverter<decimal>
	{
		private readonly CultureInfo cultureInfo = new(culture);

		public decimal Read(CsvReader reader, int index)
		{
			var text = reader.GetString(index);
			if (string.IsNullOrEmpty(text)) { return default; }
			return decimal.Parse(text, NumberStyles.Currency, cultureInfo);
		}

		[ExcludeFromCodeCoverage]
		public void Write(CsvWriter writer, decimal value)
		{
			writer.WriteField(string.Empty);
		}
	}

	public sealed class EnUsCurrencyConverter() : CurrencyConverter("en-US")
	{
	}
}
