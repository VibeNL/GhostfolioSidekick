using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.Parsers.CentraalBeheer
{
	public class CentraalBeheerRecord
	{
		[DateTimeStyles(System.Globalization.DateTimeStyles.AssumeUniversal)]
		[Format("dd/MM/yyyy")]
		[Name("Boekdatum")]
		public DateTime BookingDate { get; set; }

		[Name("Soort")]
		public required string TransactionType { get; set; }

		[Name("Fondsnaam")]
		public string? FundName { get; set; }

		[DateTimeStyles(System.Globalization.DateTimeStyles.AssumeUniversal)]
		[Format("dd/MM/yyyy")]
		[Name("Transactiedatum")]
		public DateTime TransactionDate { get; set; }

		[Name("Aantal stukken")]
		[CultureInfo("nl-NL")]
		public decimal? NumberOfUnits { get; set; }

		[Name("Koers")]
		[CultureInfo("nl-NL")]
		public decimal? Rate { get; set; }

		[Name("Aankoopkosten")]
		[CultureInfo("nl-NL")]
		public decimal? PurchaseCosts { get; set; }

		[Name("Dividendbelasting")]
		[CultureInfo("nl-NL")]
		public decimal? DividendTax { get; set; }

		[Name("Af Bij")]
		public string? DebitCredit { get; set; }

		[Name("Bruto bedrag (EUR)")]
		[CultureInfo("nl-NL")]
		public decimal? GrossAmount { get; set; }

		[Name("Netto bedrag (EUR)")]
		[CultureInfo("nl-NL")]
		public decimal? NetAmount { get; set; }

		[Name("Naam")]
		public string? Name { get; set; }

		[Name("Rekeningnummer")]
		public string? AccountNumber { get; set; }

		[Name("Omschrijving")]
		public string? Description { get; set; }
	}
}