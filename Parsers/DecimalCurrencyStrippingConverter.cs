using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Globalization;
using System.Text;
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

            // Normalize Unicode to ensure symbols are recognized
            var normalized = text.Normalize(NormalizationForm.FormKC);
            // Remove all non-digit, non-decimal, non-minus characters
            var cleaned = MyRegex().Replace(normalized, "");
            // Replace comma with dot if comma is used as decimal separator
            if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var value))
            {
                return value;
            }

            throw new TypeConverterException(this, memberMapData, text, row.Context, $"Cannot convert '{text}' to decimal.");
        }

		[GeneratedRegex("[^0-9.,\\-]+", RegexOptions.Compiled, 30000)]
		private static partial Regex MyRegex();
	}
}
