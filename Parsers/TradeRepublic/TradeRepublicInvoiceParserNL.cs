using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicInvoiceParserNL : TradeRepublicInvoiceParserBase
	{
		// EN
		protected override string Keyword_Position => "POSITIE";
		protected override string Keyword_Quantity => "AANTAL";
		protected override string Keyword_Price => string.Empty;
		protected override string Keyword_Amount => "BEDRAG";
		protected override string Keyword_Nominal => string.Empty;
		protected override string Keyword_Income => "OPBRENGST";
		protected override string Keyword_Coupon => string.Empty;
		protected override string Keyword_Total => "TOTAAL";
		protected override string Keyword_AverageRate => string.Empty;
		protected override string Keyword_Booking => string.Empty;
		protected override string Keyword_Security => string.Empty;
		protected override string Keyword_Number => string.Empty;
		protected override string DATE => "DATUM";

		public TradeRepublicInvoiceParserNL(IPdfToWordsParser parsePDfToWords) : base(parsePDfToWords)
		{
		}
	}
}
