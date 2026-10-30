using CsvHelper;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Trading212
{
	[CsvRecord]
	public partial class Trading212Record
	{
		public required string Action { get; set; }

		[CsvColumn("Time (UTC)", "Time")]
		[CsvConverter(typeof(UtcDateTimeConverter))]
		public DateTime Time { get; set; }

		public string? ISIN { get; set; }

		[ExcludeFromCodeCoverage]
		public string? Ticker { get; set; }

		[ExcludeFromCodeCoverage]
		public string? Name { get; set; }

		[CsvColumn("No. of shares")]
		public decimal? NumberOfShares { get; set; }

		[CsvColumn("Price / share")]
		[CsvConverter(typeof(ExponentNullableDecimalConverter))]
		public decimal? Price { get; set; }

		[CsvColumn("Currency (Price / share)")]
		public string? Currency { get; set; }

		[ExcludeFromCodeCoverage]
		[CsvColumn("Exchange rate", Optional = true)]
		[CsvConverter(typeof(ExchangeRateConverter))]
		public decimal? ExchangeRate { get; set; }

		[ExcludeFromCodeCoverage]
		[CsvColumn("Currency (Result)", Optional = true)]
		public string? CurrencySource { get; set; }

		[CsvColumn("Stamp duty reserve tax", Optional = true)]
		public decimal? TaxUK { get; set; }

		[CsvColumn("Currency (Stamp duty reserve tax)", Optional = true)]
		public string? TaxUKCurrency { get; set; }

		[CsvColumn("French transaction tax", Optional = true)]
		public decimal? TaxFrance { get; set; }

		[CsvColumn("Currency (French transaction tax)", Optional = true)]
		public string? TaxFranceCurrency { get; set; }

		[CsvColumn("Finra fee", Optional = true)]
		public decimal? FeeFinra { get; set; }

		[CsvColumn("Currency (Finra fee)", Optional = true)]
		public string? FeeFinraCurrency { get; set; }

		public string? Notes { get; set; }

		[CsvColumn("ID")]
		public string? Id { get; set; }

		[CsvColumn("Currency conversion fee", Optional = true)]
		public decimal? ConversionFee { get; set; }

		[CsvColumn("Currency (Currency conversion fee)", Optional = true)]
		public string? ConversionFeeCurrency { get; set; }

		public decimal? Total { get; set; }

		[CsvColumn("Currency (Total)")]
		public string? CurrencyTotal { get; set; }
	}

	internal sealed class ExchangeRateConverter : ICsvConverter<decimal?>
	{
		public decimal? Read(CsvReader reader, int index)
		{
			var text = reader.GetString(index);
			return text switch
			{
				"" => null,
				"Not available" => null,
				_ => decimal.Parse(text),
			};
		}

		[ExcludeFromCodeCoverage]
		public void Write(CsvWriter writer, decimal? value)
		{
			writer.WriteField(value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
		}
	}
}
