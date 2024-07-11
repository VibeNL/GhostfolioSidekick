using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicInvoiceParserEN : TradeRepublicInvoiceParserBase
	{
		// EN
		protected override string Keyword_Position => "POSITION";
		protected override string Keyword_Quantity => "QUANTITY";
		protected override string Keyword_Price => "PRICE";
		protected override string Keyword_Amount => "AMOUNT";
		protected override string Keyword_Nominal => "NOMINAL";
		protected override string Keyword_Income => "INCOME";
		protected override string Keyword_Coupon => "COUPON";
		protected override string Keyword_Total => "TOTAL";
		protected override string Keyword_AverageRate => "AVERAGE RATE";
		protected override string Keyword_Booking => "BOOKING";
		protected override string Keyword_Security => "SECURITY";
		protected override string Keyword_Number => "NO.";
		protected override string DATE => "DATE";

		public TradeRepublicInvoiceParserEN(IPdfToWordsParser parsePDfToWords) : base(parsePDfToWords)
		{
		}
	}
}
