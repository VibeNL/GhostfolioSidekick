using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

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
		protected override string[] Keyword_Booking => ["BOOKING"];
		protected override string Keyword_Security => "SECURITY";
		protected override string Keyword_Number => "NO.";
		protected override string SECURITIES_SETTLEMENT => "SECURITIES SETTLEMENT";
		protected override string DIVIDEND => "DIVIDEND";
		protected override string INTEREST_PAYMENT => "INTEREST PAYMENT";
		protected override string REPAYMENT => "REPAYMENT";
		protected override string ACCRUED_INTEREST => "Accrued interest";
		protected override string EXTERNAL_COST_SURCHARGE => "External cost surcharge";
		protected override string WITHHOLDING_TAX => "Withholding tax for US issuer";
		protected override string DATE => "DATE";
		protected override CultureInfo CULTURE => CultureInfo.InvariantCulture;

		public TradeRepublicInvoiceParserEN(IPdfToWordsParser parsePDfToWords) : base(parsePDfToWords)
		{
		}

	}
}
