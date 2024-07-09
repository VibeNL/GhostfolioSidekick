using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;
using System.Text.RegularExpressions;

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
