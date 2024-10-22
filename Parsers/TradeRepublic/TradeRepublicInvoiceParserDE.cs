using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicInvoiceParserDE : TradeRepublicInvoiceParserBase
	{
		// DE
		protected override string Keyword_Position => "POSITION";
		protected override string Keyword_Quantity => "ANZAHL";
		protected override string Keyword_Price => "PREIS";
		protected override string Keyword_Amount => "BETRAG";
		protected override string Keyword_Nominal => string.Empty;
		protected override string Keyword_Income => "ERTRAG";
		protected override string Keyword_Coupon => string.Empty;
		protected override string Keyword_Total => "GESAMT";
		protected override string Keyword_AverageRate => "DURCHSCHNITTSKURS";
		protected override string Keyword_Booking => "BUCHUNG";
		protected override string Keyword_Security => string.Empty;
		protected override string Keyword_Number => string.Empty;
		protected override string SECURITIES_SETTLEMENT => "WERTPAPIERABRECHNUNG";
		protected override string DIVIDEND => "DIVIDENDE";
		protected override string INTEREST_PAYMENT => string.Empty;
		protected override string REPAYMENT => string.Empty;
		protected override string ACCRUED_INTEREST => string.Empty;
		protected override string EXTERNAL_COST_SURCHARGE => "Fremdkostenzuschlag";
		protected override string WITHHOLDING_TAX => string.Empty;
		protected override string DATE => "DATUM";
		protected override CultureInfo CULTURE => new CultureInfo("de");

		public TradeRepublicInvoiceParserDE(IPdfToWordsParser parsePDfToWords) : base(parsePDfToWords)
		{
		}
	}
}
