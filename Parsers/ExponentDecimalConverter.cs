using CsvHelper;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GhostfolioSidekick.Parsers
{
	public sealed class ExponentDecimalConverter : ICsvConverter<decimal>
	{
		private const NumberStyles Styles = NumberStyles.Number | NumberStyles.AllowExponent;

		public decimal Read(CsvReader reader, int index)
		{
			return decimal.Parse(reader.GetString(index), Styles, CultureInfo.InvariantCulture);
		}

		[ExcludeFromCodeCoverage]
		public void Write(CsvWriter writer, decimal value)
		{
			writer.WriteField(value.ToString(CultureInfo.InvariantCulture));
		}

		internal static decimal? ParseNullable(string text)
		{
			return string.IsNullOrWhiteSpace(text) ? null : decimal.Parse(text, Styles, CultureInfo.InvariantCulture);
		}
	}

	public sealed class ExponentNullableDecimalConverter : ICsvConverter<decimal?>
	{
		public decimal? Read(CsvReader reader, int index)
		{
			return ExponentDecimalConverter.ParseNullable(reader.GetString(index));
		}

		[ExcludeFromCodeCoverage]
		public void Write(CsvWriter writer, decimal? value)
		{
			writer.WriteField(value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
		}
	}
}
