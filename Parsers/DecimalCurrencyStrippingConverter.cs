using CsvHelper;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.Coinbase
{
	public sealed partial class DecimalCurrencyStrippingConverter : ICsvConverter<decimal?>
	{
		public decimal? Read(CsvReader reader, int index)
		{
			var text = reader.GetString(index);
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}

			// Normalize Unicode to ensure symbols are recognized
			var normalized = text.Normalize(NormalizationForm.FormKC);
			// Remove all non-digit, non-decimal, non-minus characters
			var cleaned = MyRegex().Replace(normalized, "");

			if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var value))
			{
				return value;
			}

			throw new FormatException($"Cannot convert '{text}' to decimal (column {index}).");
		}

		[ExcludeFromCodeCoverage]
		public void Write(CsvWriter writer, decimal? value)
		{
			writer.WriteField(value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
		}

		[GeneratedRegex("[^0-9.,-]+", RegexOptions.Compiled, 30000)]
		private static partial Regex MyRegex();
	}
}
