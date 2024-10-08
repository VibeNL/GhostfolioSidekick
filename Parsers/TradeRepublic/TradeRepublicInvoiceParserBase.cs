﻿using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.ISIN;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public abstract class TradeRepublicInvoiceParserBase : PdfBaseParser
	{
		protected abstract string Keyword_Position { get; }
		protected abstract string Keyword_Quantity { get; }
		protected abstract string Keyword_Price { get; }
		protected abstract string Keyword_Amount { get; }
		protected abstract string Keyword_Nominal { get; }
		protected abstract string Keyword_Income { get; }
		protected abstract string Keyword_Coupon { get; }
		protected abstract string Keyword_Total { get; }
		protected abstract string Keyword_AverageRate { get; }
		protected abstract string Keyword_Booking { get; }
		protected abstract string Keyword_Security { get; }
		protected abstract string Keyword_Number { get; }
		protected abstract string DATE { get; }

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

		protected TradeRepublicInvoiceParserBase(IPdfToWordsParser parsePDfToWords) : base(parsePDfToWords)
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Not needed for now")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S127:\"for\" loop stop conditions should be invariant", Justification = "Needed for parser")]
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
				if (word.Text == DATE && dateTime == null)
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
							i += keywordSplitted.Length - 1;
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

		private int ParseSecurityRecord(List<SingleWordToken> words, int i, DateTime dateTime, List<MultiWordToken> headers, List<PartialActivity> activities)
		{
			var headerStrings = headers.Select(h => h.KeyWord).ToList();
			if (headerStrings.Contains(Keyword_Quantity) && (headerStrings.Contains(Keyword_Price) || headerStrings.Contains(Keyword_AverageRate))) // Stocks
			{
				var isin = GetIsin(words, ref i);
				string id = GetId(dateTime, isin);

				activities.Add(PartialActivity.CreateBuy(
					new Currency(words[i + 4].Text),
					dateTime,
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					decimal.Parse(words[i + 1].Text, CultureInfo.InvariantCulture),
					decimal.Parse(words[i + 3].Text, CultureInfo.InvariantCulture),
					new Money(new Currency(words[i + 4].Text), decimal.Parse(words[i + 5].Text, CultureInfo.InvariantCulture)),
					id));

				return i + 6;
			}

			if (headerStrings.Contains(Keyword_Nominal) && headerStrings.Contains(Keyword_Price)) // Bonds
			{
				var isin = GetIsin(words, ref i);
				string id = GetId(dateTime, isin);

				activities.Add(PartialActivity.CreateBuy(
					new Currency(words[i + 6].Text),
					dateTime,
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					decimal.Parse(words[i + 1].Text, CultureInfo.InvariantCulture),
					decimal.Parse(words[i + 3].Text, CultureInfo.InvariantCulture) / 100,
					new Money(new Currency(words[i + 6].Text), decimal.Parse(words[i + 5].Text, CultureInfo.InvariantCulture)),
					id));

				return i + 6;
			}

			if (headerStrings.Contains(Keyword_Income) || headerStrings.Contains(Keyword_Nominal)) // Dividends
			{
				var isin = GetIsin(words, ref i);
				string id = GetId(dateTime, isin);

				activities.Add(PartialActivity.CreateDividend(
					new Currency(words[i + 6].Text),
					dateTime,
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					decimal.Parse(words[i + 5].Text, CultureInfo.InvariantCulture),
					new Money(new Currency(words[i + 6].Text), decimal.Parse(words[i + 5].Text, CultureInfo.InvariantCulture)),
					id));

				return i + 6;
			}

			if (headerStrings.Contains(Keyword_Security) && headerStrings.Contains(Keyword_Booking)) // Repay of bonds
			{
				i = i + 2; // skip column "Number" and "Booking"
				var isin = GetIsin(words, ref i);
				string id = GetId(dateTime, isin);

				activities.Add(PartialActivity.CreateBondRepay(
					new Currency(words[i + 2].Text),
					dateTime,
					[PartialSymbolIdentifier.CreateStockBondAndETF(isin)],
					decimal.Parse(words[i + 1].Text, CultureInfo.InvariantCulture),
					new Money(new Currency(words[i + 2].Text), decimal.Parse(words[i + 1].Text, CultureInfo.InvariantCulture)),
					id));

				return i + 2;
			}

			throw new NotSupportedException("Unknown security type");

			static string GetId(DateTime dateTime, string isin)
			{
				return $"Trade_Republic_{isin}_{dateTime.ToInvariantDateOnlyString()}";
			}
		}

		private static string GetIsin(List<SingleWordToken> words, ref int i)
		{
			var sourceI = i;
			string? isin = null;
			while (i < words.Count)
			{
				if (words[i].Text == "ISIN:")
				{
					isin = words[i + 1].Text;
					i++;
					return isin;
				}

				i++;
			}

			// Detect via pattern
			i = sourceI;
			while (i < words.Count)
			{
				if (Isin.ValidateCheckDigit(words[i].Text))
				{
					isin = words[i].Text;
					return isin;
				}

				i++;
			}

			throw new NotSupportedException("ISIN not found");
		}
	}
}
