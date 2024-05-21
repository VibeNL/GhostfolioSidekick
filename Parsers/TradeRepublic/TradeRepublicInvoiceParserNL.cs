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
		private const string Keyword_Nominal = "NOMINAL";
		private const string Keyword_Total = "TOTAL";
		private const string Keyword_AverageRate = "AVERAGE RATE";

		private List<string> TableKeyWords
		{
			get
			{
				return [
					Keyword_Position,
					Keyword_Quantity,
					Keyword_Nominal,
					Keyword_Price,
					Keyword_AverageRate,
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
			DateTime? dateTime = null;
			bool inHeader = false;
			var uniqueId = string.Empty;

			for (int i = 0; i < words.Count; i++)
			{
				var word = words[i];

				// Detect first date
				if (word.Text == "DATE" && dateTime == null)
				{
					var date = words[i + 1].Text;
					dateTime = DateTime.ParseExact(date, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
					i += 1;
				}

				if (Keyword_Total == word.Text) // start of header
				{
					headers.Clear();
				}

				if (!inHeader && headers.Count == 4) // parsing rows buys and sells
				{
					var r = ParseSecurityRecord(words, i, dateTime.GetValueOrDefault(), headers, activities);
					i = r.Item1;
					uniqueId = r.Item2;
				}

				if (!inHeader && headers.Count == 2)
				{
					i = ParseFeeRecords(words, i, dateTime.GetValueOrDefault(), uniqueId, activities);
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

		private static int ParseFeeRecords(List<SingleWordToken> words, int i, DateTime dateTime, string uniqueId, List<PartialActivity> activities)
		{
			var skip = 0;
			if (words[i].Text == "Accrued" && words[i + 1].Text == "interest")
			{
				skip = 2;
			}
			else if (words[i].Text == "External" && words[i + 1].Text == "cost" && words[i + 2].Text == "surcharge")
			{
				skip = 3;
			}

			var price = Math.Abs(decimal.Parse(words[i + skip].Text, CultureInfo.InvariantCulture));
			var currencySymbol = words[i + skip + 1].Text;
			var currency = new Currency(currencySymbol);

			activities.Add(PartialActivity.CreateFee(
					currency,
					dateTime,
					price,
					new Money(currency, price),
					uniqueId));

			return i + skip + 1;
		}

		private static (int, string) ParseSecurityRecord(List<SingleWordToken> words, int i, DateTime dateTime, List<MultiWordToken> headers, List<PartialActivity> activities)
		{
			var headerStrings = headers.Select(h => h.KeyWord).ToList();
			if (headerStrings.Contains(Keyword_Quantity) && (headerStrings.Contains(Keyword_Price) || headerStrings.Contains(Keyword_AverageRate))) // Stocks
			{
				string? isin = null;
				while (i < words.Count)
				{
					if (words[i].Text == "ISIN:")
					{
						isin = words[i + 1].Text;
						i++;
						break;
					}

					i++;
				}

				if (isin == null)
				{
					throw new NotSupportedException("ISIN not found");
				}

				var id = $"Trade_Republic_{isin}_{dateTime.ToInvariantDateOnlyString()}";

				var quantity = decimal.Parse(words[i + 1].Text, CultureInfo.InvariantCulture);
				var price = decimal.Parse(words[i + 3].Text, CultureInfo.InvariantCulture);
				var currencySymbol = words[i + 4].Text;
				var total = decimal.Parse(words[i + 5].Text, CultureInfo.InvariantCulture);

				var currency = new Currency(currencySymbol);

				activities.Add(PartialActivity.CreateBuy(
					currency,
					dateTime,
					[PartialSymbolIdentifier.CreateStockAndETF(isin)],
					quantity,
					price,
					new Money(currency, total),
					id));

				return (i + 6, id);
			}

			if (headerStrings.Contains(Keyword_Nominal)) // Bonds
			{
				string? isin = null;
				while (i < words.Count)
				{
					if (words[i].Text == "ISIN:")
					{
						isin = words[i + 1].Text;
						i++;
						break;
					}

					i++;
				}

				if (isin == null)
				{
					throw new NotSupportedException("ISIN not found");
				}

				var id = $"Trade_Republic_{isin}_{dateTime.ToInvariantDateOnlyString()}";

				var nominal = decimal.Parse(words[i + 1].Text, CultureInfo.InvariantCulture);
				var price = decimal.Parse(words[i + 3].Text, CultureInfo.InvariantCulture);
				var currencySymbol = words[i + 6].Text;
				var total = decimal.Parse(words[i + 5].Text, CultureInfo.InvariantCulture);

				var currency = new Currency(currencySymbol);

				activities.Add(PartialActivity.CreateBuy(
					currency,
					dateTime,
					[PartialSymbolIdentifier.CreateStockAndETF(isin)],
					nominal,
					price / 100,
					new Money(currency, total),
					id));

				return (i + 6, id);
			}

			throw new NotSupportedException("Unknown security type");
		}
	}
}
