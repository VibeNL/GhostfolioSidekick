using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;
using System.Text.RegularExpressions;

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

		protected override bool CanParseRecords(List<SingleWordToken> words)
		{
			var foundTradeRepublic = false;
			var foundSecurities = false;
			var foundLanguage = false;

			for (int i = 0; i < words.Count; i++)
			{
				if (IsCheckWords("TRADE REPUBLIC BANK GMBH", words, i))
				{
					foundTradeRepublic = true;
				}

				if (IsCheckWords(DATE, words, i))
				{
					foundLanguage = true;
				}

				if (
					IsCheckWords("SECURITIES SETTLEMENT", words, i) ||
					IsCheckWords("DIVIDEND", words, i) ||
					IsCheckWords("INTEREST PAYMENT", words, i) ||
					IsCheckWords("REPAYMENT", words, i))
				{
					foundSecurities = true;
				}
			}

			return foundLanguage && foundTradeRepublic && foundSecurities;
		}
	}
}
