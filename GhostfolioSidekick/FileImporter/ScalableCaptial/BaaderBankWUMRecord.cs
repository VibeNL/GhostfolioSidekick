using CsvHelper.Configuration.Attributes;

namespace GhostfolioSidekick.FileImporter.ScalableCaptial
{
	public class BaaderBankWUMRecord
    {
        [Name("XXX-BUDAT")]
        [Format("yyyyMMdd")]
        public DateOnly Date { get; set; }

        [Name("XXX-WPKURS")]
        [CultureInfo("nl-NL")]
        public decimal? UnitPrice { get; set; }

        [Name("XXX-WHGAB")]
        public string Currency { get; set; }

        [Name("XXX-NW")]
        [CultureInfo("nl-NL")]
        public decimal? Quantity { get; set; }

        [Name("XXX-WPNR")]
        public string Isin { get; set; }

        [Name("XXX-WPGART")]
        public string OrderType { get; set; }

        [Name("XXX-EXTORDID")]
        public string Reference { get; set; }

        [Name("XXX-BELEGU")]
        public string SavingsPlanIdentification { get; set; }
    }
}
