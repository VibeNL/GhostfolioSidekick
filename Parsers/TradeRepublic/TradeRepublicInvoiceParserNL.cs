﻿using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicInvoiceParserNL : PdfBaseParser
	{
		private const string Keyword_Position = "POSITION";
		private const string Keyword_Quantity = "QUANTITY";
		private const string Keyword_Price = "PRICE";
		private const string Keyword_Amount = "AMOUNT";
		private const string Keyword_Nominal = "NOMINAL";
		private const string Keyword_Income = "INCOME";
		private const string Keyword_Coupon = "COUPON";
		private const string Keyword_Total = "TOTAL";
		private const string Keyword_AverageRate = "AVERAGE RATE";
		private const string Keyword_Booking = "BOOKING";
		private const string Keyword_Security = "SECURITY";
		private const string Keyword_Number = "NO.";

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
					Keyword_Income,
					Keyword_Coupon,
					Keyword_Amount,
					Keyword_Booking,
					Keyword_Security,
					Keyword_Number
				];
			}
		}

		public TradeRepublicInvoiceParserNL(IPdfToWordsParser parsePDfToWords) : base(parsePDfToWords)
		{
		}

		protected override bool CanParseRecords(List<SingleWordToken> words)
		{
			var foundTradeRepublic = false;
			var foundSecurities = false;

			for (int i = 0; i < words.Count; i++)
			{
				if (IsCheckWords("TRADE REPUBLIC BANK GMBH", words, i))
				{
					foundTradeRepublic = true;
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

			return foundTradeRepublic && foundSecurities;
		}

		protected override List<PartialActivity> ParseRecords(List<SingleWordToken> words)
		{
			var activities = new List<PartialActivity>();

			// detect headers
			var headers = new List<MultiWordToken>();
			DateTime? dateTime = null;
			bool inHeader = false;

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
					i = ParseSecurityRecord(words, i, dateTime.GetValueOrDefault(), headers, activities);
				}

				if (!inHeader && headers.Count == 2) // parsing fees
				{
					i = ParseFeeRecords(words, i, dateTime.GetValueOrDefault(), activities);
				}

				if (Keyword_Position == word.Text || Keyword_Number == word.Text) // start of header
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

			var mainActivity = activities.Single(x => !string.IsNullOrWhiteSpace(x.TransactionId));
			activities.ToList().ForEach(x => x.TransactionId = mainActivity.TransactionId);

			return activities;
		}

		private static int ParseFeeRecords(List<SingleWordToken> words, int i, DateTime dateTime, List<PartialActivity> activities)
		{
			int skip;
			if (IsCheckWords("Accrued interest", words, i))
			{
				skip = 2;
			}
			else if (IsCheckWords("External cost surcharge", words, i))
			{
				skip = 3;
			}
			else if (IsCheckWords("Withholding tax for US issuer", words, i))
			{
				skip = 5;
			}
			else
			{
				return i;
			}

			var price = Math.Abs(decimal.Parse(words[i + skip].Text, CultureInfo.InvariantCulture));
			var currencySymbol = words[i + skip + 1].Text;
			var currency = new Currency(currencySymbol);

			activities.Add(PartialActivity.CreateFee(
					currency,
					dateTime,
					price,
					new Money(currency, price),
					string.Empty));

			return i + skip + 1;
		}

		private static int ParseSecurityRecord(List<SingleWordToken> words, int i, DateTime dateTime, List<MultiWordToken> headers, List<PartialActivity> activities)
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
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					quantity,
					price,
					new Money(currency, total),
					id));

				return i + 6;
			}

			if (headerStrings.Contains(Keyword_Nominal) && headerStrings.Contains(Keyword_Price)) // Bonds
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
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					nominal,
					price / 100,
					new Money(currency, total),
					id));

				return i + 6;
			}

			if (headerStrings.Contains(Keyword_Income) || headerStrings.Contains(Keyword_Nominal)) // Dividends
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
				var total = decimal.Parse(words[i + 5].Text, CultureInfo.InvariantCulture);
				var currencySymbol = words[i + 6].Text;

				var currency = new Currency(currencySymbol);

				activities.Add(PartialActivity.CreateDividend(
					currency,
					dateTime,
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					total,
					new Money(currency, total),
					id));

				return i + 6;
			}

			if (headerStrings.Contains(Keyword_Security) && headerStrings.Contains(Keyword_Booking)) // Repay of bonds
			{
				i = i + 2; // skip column "Number" and "Booking"
				string? isin = null;
				while (i < words.Count)
				{
					if (Regex.IsMatch(words[i].Text, "\\(20..\\)"))
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

				var total = decimal.Parse(words[i + 1].Text, CultureInfo.InvariantCulture);
				var currencySymbol = words[i + 2].Text;

				var currency = new Currency(currencySymbol);

				activities.Add(PartialActivity.CreateBondRepay(
					currency,
					dateTime,
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					total,
					new Money(currency, total),
					id));

				return i + 2;
			}

			throw new NotSupportedException("Unknown security type");
		}
	}
}
