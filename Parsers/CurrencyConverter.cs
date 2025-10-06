using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GhostfolioSidekick.Parsers
{
	public class CurrencyConverter : DefaultTypeConverter
	{
		private readonly CultureInfo cultureInfo;

		public CurrencyConverter(string culture)
		{
			this.cultureInfo = new CultureInfo(culture);
		}

		public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
		{
			if (text == null) { return null; }
			return decimal.Parse(text, NumberStyles.Currency, cultureInfo);
		}

		[ExcludeFromCodeCoverage]
		public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
		{
			return string.Empty;
		}
	}

}
