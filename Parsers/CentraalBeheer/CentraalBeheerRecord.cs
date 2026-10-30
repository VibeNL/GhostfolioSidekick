using CsvHelper;

namespace GhostfolioSidekick.Parsers.CentraalBeheer
{
	[CsvRecord]
	public partial class CentraalBeheerRecord
	{
		[CsvColumn("Boekdatum")]
		[CsvConverter(typeof(DayFirstUtcDateTimeConverter))]
		public DateTime BookingDate { get; set; }

		[CsvColumn("Soort")]
		public required string TransactionType { get; set; }

		[CsvColumn("Fondsnaam")]
		public string? FundName { get; set; }

		[CsvColumn("Transactiedatum")]
		[CsvConverter(typeof(DayFirstUtcDateTimeConverter))]
		public DateTime TransactionDate { get; set; }

		[CsvColumn("Aantal stukken")]
		[CsvCulture("nl-NL")]
		public decimal? NumberOfUnits { get; set; }

		[CsvColumn("Koers")]
		[CsvCulture("nl-NL")]
		public decimal? Rate { get; set; }

		[CsvColumn("Aankoopkosten")]
		[CsvCulture("nl-NL")]
		public decimal? PurchaseCosts { get; set; }

		[CsvColumn("Dividendbelasting")]
		[CsvCulture("nl-NL")]
		public decimal? DividendTax { get; set; }

		[CsvColumn("Af Bij")]
		public string? DebitCredit { get; set; }

		[CsvColumn("Bruto bedrag (EUR)")]
		[CsvCulture("nl-NL")]
		public decimal? GrossAmount { get; set; }

		[CsvColumn("Netto bedrag (EUR)")]
		[CsvCulture("nl-NL")]
		public decimal? NetAmount { get; set; }

		[CsvColumn("Naam")]
		public string? Name { get; set; }

		[CsvColumn("Rekeningnummer")]
		public string? AccountNumber { get; set; }

		[CsvColumn("Omschrijving")]
		public string? Description { get; set; }
	}
}
