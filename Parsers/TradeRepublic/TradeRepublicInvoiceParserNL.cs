﻿using GhostfolioSidekick.Model;
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

		private List<string> TableKeyWords
		{
			get
			{
				return [
					Keyword_Position,
					Keyword_Quantity,
					Keyword_Price,
					Keyword_Amount
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

				if (headers.Count == TableKeyWords.Count) // parsing rows
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
			for (int j = i; j < words.Count - 2; j++)
			{
				if (DateTime.TryParseExact(
					words[j].Text + " " + words[j + 1].Text + " " + words[j + 2].Text,
					"dd MMM yyyy",
					new CultureInfo("NL-nl"),
					DateTimeStyles.AssumeUniversal,
					out var date))
				{
					// start of a new activity
					SingleWordToken singleWordToken = words[j + 3];
					if (singleWordToken.Text == "Handel" || singleWordToken.Text == "Pagina")
					{
						return j - i + 3;
					}

					var items = words.Skip(j + 4).TakeWhile(w => w.BoundingBox!.Row == singleWordToken.BoundingBox!.Row).ToList();

					var amountText = items[items.Count - 2];
					var currency = new Model.Currency(CurrencyTools.GetCurrencyFromSymbol(amountText.Text.Substring(0, 1)));
					var amount = decimal.Parse(amountText.Text.Substring(1).Trim(), new CultureInfo("nl-NL"));

					var id = $"Trade_Republic_{singleWordToken.Text}_{date.ToInvariantDateOnlyString()}";

					switch (singleWordToken.Text)
					{
						case "Rentebetaling":
							activities.Add(PartialActivity.CreateInterest(
											currency,
											date,
											amount,
											string.Join(" ", items.Take(items.Count - 2)),
											new Money(currency, amount),
											id));

							break;
						case "Overschrijving":
							activities.Add(PartialActivity.CreateCashDeposit(
											currency,
											date,
											amount,
											new Money(currency, amount),
											id));
							break;
						case "Kaarttransactie":
							activities.Add(PartialActivity.CreateCashWithdrawal(
												currency,
												date,
												amount,
												new Money(currency, amount),
												id));
							break;
						case "Beloning":
							activities.Add(PartialActivity.CreateGift(
													currency,
													date,
													amount,
													new Money(currency, amount),
													id));
							break;
						case "Handel":
							// Should be handeld by another parser
							break;
						default:
							throw new NotSupportedException();
					}

					return j - i + 3 + items.Count;
				}
			}

			return int.MaxValue;
		}
	}
}
