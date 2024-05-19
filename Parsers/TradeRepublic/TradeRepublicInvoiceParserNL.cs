using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicInvoiceParserNL : PdfBaseParser, IFileImporter
	{
		private const string Keyword_Position = "POSITION";
		private const string Keyword_Quantity = "QUANTITY";
		private const string Keyword_Price = "PRICE";
		private const string Keyword_Amount = "AMOUNT";
		private const string Keyword_NominaL = "NOMINAL";

		private List<string> TableKeyWords
		{
			get
			{
				return [
					Keyword_Position,
					Keyword_Quantity,
					Keyword_NominaL,
					Keyword_Price,
					Keyword_Amount,
				];
			}
		}

		public TradeRepublicInvoiceParserNL(IPdfToWordsParser parsePDfToWords) : base(parsePDfToWords)
		{
		}

		protected override List<PartialActivity> ParseRecords(List<SingleWordToken> words)
		{
			var activities = new List<PartialActivity>();

			// detect headers
			var headers = new List<MultiWordToken>();

			bool inHeader = false;

			for (int i = 0; i < words.Count; i++)
			{
				var word = words[i];

				if (headers.Count == 4) // parsing rows
				{
					var incr = ParseActivity(words, i, activities);
					if (incr == int.MaxValue)
					{
						break;
					}

#pragma warning disable S127 // "for" loop stop conditions should be invariant
					i += incr;
#pragma warning restore S127 // "for" loop stop conditions should be invariant
				}

				if (Keyword_Position == word.Text) // start of header
				{
					inHeader = true;
				}

				if (inHeader) // add column headers
				{
					var matched = false;
					foreach (var kw in TableKeyWords)
					{
						var keywordMatch = true;
						string[] keywordSplitted = kw.Split(" ");
						for (int j = 0; j < keywordSplitted.Length; j++)
						{
							string? keyword = keywordSplitted[j];
							if (words[i + j].Text != keyword)
							{
								keywordMatch = false;
								break;
							}
						}

						if (keywordMatch)
						{
							headers.Add(new MultiWordToken(kw, word.BoundingBox));
							matched = true;
#pragma warning disable S127 // "for" loop stop conditions should be invariant
							i += keywordSplitted.Length - 1;
#pragma warning restore S127 // "for" loop stop conditions should be invariant
							break;
						}
					}

					if (!matched)
					{
						inHeader = false;
						headers.Clear();
					}
				}

				if (Keyword_Amount == word.Text) // end of header
				{
					inHeader = false;
				}
			}

			return activities;
		}

		private static int ParseActivity(List<SingleWordToken> words, int i, List<PartialActivity> activities)
		{
			throw new NotSupportedException();
		}
	}
}
