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
			var cleaned = ExponentRegex().IsMatch(normalized)
				? ExponentStripRegex().Replace(normalized, "")
				: MyRegex().Replace(normalized, "");

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

		// A number in scientific notation, e.g. 1.5E-05
		[GeneratedRegex(@"[0-9.][eE][+-]?[0-9]", RegexOptions.Compiled, 30000)]
		private static partial Regex ExponentRegex();

		// Keeps the exponent marker and sign; also drops a currency code like EUR next to the number
		[GeneratedRegex(@"[^0-9.,+eE-]+|(?<![0-9.])[eE]", RegexOptions.Compiled, 30000)]
		private static partial Regex ExponentStripRegex();
	}
}
