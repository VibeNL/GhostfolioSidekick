using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Parsers.Trading212
{
	public class Trading212Record
	{
		public required string Action { get; set; }

		[DateTimeStyles(System.Globalization.DateTimeStyles.AssumeUniversal)]
		public DateTime Time { get; set; }

		public string? ISIN { get; set; }

		[ExcludeFromCodeCoverage]
		public string? Ticker { get; set; }

		[ExcludeFromCodeCoverage]
		public string? Name { get; set; }

		[Name("No. of shares")]
		public decimal? NumberOfShares { get; set; }

		[Name("Price / share")]
		public decimal? Price { get; set; }

		[Name("Currency (Price / share)")]
		public string? Currency { get; set; }

		[ExcludeFromCodeCoverage]
		[Optional]
		[Name("Exchange rate")]
		[TypeConverter(typeof(ExchangeRateConverter))]
		public decimal? ExchangeRate { get; set; }

		[ExcludeFromCodeCoverage]
		[Optional]
		[Name("Currency (Result)")]
		public string? CurrencySource { get; set; }

		[Optional]
		[Name("Stamp duty reserve tax")]
		public decimal? FeeUK { get; set; }

		[Optional]
		[Name("Currency (Stamp duty reserve tax)")]
		public string? FeeUKCurrency { get; set; }

		[Optional]
		[Name("French transaction tax")]
		public decimal? FeeFrance { get; set; }

		[Optional]
		[Name("Currency (French transaction tax)")]
		public string? FeeFranceCurrency { get; set; }

		public string? Notes { get; set; }

		[Name("ID")]
		public string? Id { get; set; }

		[Optional]
		[Name("Currency conversion fee")]
		public decimal? ConversionFee { get; set; }

		[Optional]
		[Name("Currency (Currency conversion fee)")]
		public string? ConversionFeeCurrency { get; set; }

		public decimal? Total { get; set; }

		[Name("Currency (Total)")]
		public string? CurrencyTotal { get; set; }


	}

	internal class ExchangeRateConverter : DefaultTypeConverter
	{
		public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
		{
			return text switch
			{
				null or "" => null,
				"Not available" => null,
				_ => decimal.Parse(text),
			};
		}
	}
}
