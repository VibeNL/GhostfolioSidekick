using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.Coinbase
{
    public partial class DecimalCurrencyStrippingConverter : DefaultTypeConverter
    {
        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            // Remove currency symbols and whitespace
            var cleaned = MyRegex().Replace(text, "");
            if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new TypeConverterException(this, memberMapData, text, row.Context, $"Cannot convert '{text}' to decimal.");
        }

		[GeneratedRegex("[\\p{Sc}\\s]")]
		private static partial Regex MyRegex();
	}
}
