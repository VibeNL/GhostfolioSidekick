using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicInvoiceParserNL(IPdfToWordsParser parsePDfToWords) : TradeRepublicInvoiceParserBase(parsePDfToWords)
	{
		// EN
		protected override string Keyword_Position => "POSITIE";
		protected override string Keyword_Quantity => "AANTAL";
		protected override string Keyword_Quantity_PiecesText => string.Empty;
		protected override string[] Keyword_Price => [string.Empty];
		protected override string Keyword_Amount => "BEDRAG";
		protected override string[] Keyword_Nominal => [string.Empty];
		protected override string Keyword_Income => "OPBRENGST";
		protected override string Keyword_Coupon => string.Empty;
		protected override string Keyword_Total => "TOTAAL";
		protected override string Keyword_AverageRate => string.Empty;
		protected override string[] Keyword_Booking => [string.Empty];
		protected override string Keyword_Security => string.Empty;
		protected override string Keyword_Number => string.Empty;
		protected override string SECURITIES_SETTLEMENT => "SECURITIES SETTLEMENT";
		protected override string DIVIDEND => "DIVIDEND";
		protected override string INTEREST_PAYMENT => "INTEREST PAYMENT";
		protected override string REPAYMENT => "REPAYMENT";
		protected override string ACCRUED_INTEREST => "Accrued interest";
		protected override string EXTERNAL_COST_SURCHARGE => "External cost surcharge";
		protected override string WITHHOLDING_TAX => "Withholding tax for US issuer";
		protected override string DATE => "DATUM";
		protected override CultureInfo Culture => CultureInfo.InvariantCulture;
	}
}
