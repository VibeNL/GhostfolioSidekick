using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using CsvHelper;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Parsers
{
	public class CurrencyConverter : DefaultTypeConverter
	{
		private readonly CultureInfo cultureInfo;

		public CurrencyConverter(string culture)
		{
			cultureInfo = new CultureInfo(culture);
		}

		public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
		{
			return decimal.Parse(text, NumberStyles.Currency, cultureInfo);
		}

		[ExcludeFromCodeCoverage]
		public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
		{
			return string.Empty;
		}
	}

}
