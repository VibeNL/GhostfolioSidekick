using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public class TradeRepublicStatementParserNL(IPdfToWordsParser parsePDfToWords) : PdfBaseParser(parsePDfToWords)
	{
		private const string Keyword_Datum = "DATUM";
		private const string Keyword_Type = "TYPE";
		private const string Keyword_Beschrijving = "BESCHRIJVING";
		private const string Keyword_BedragBij = "BEDRAG BIJ";
		private const string Keyword_BedragAf = "BEDRAF AF";
		private const string Keyword_Saldo = "SALDO";

		private static List<string> TableKeyWords
		{
			get
			{
				return [
					Keyword_Datum,
					Keyword_Type,
					Keyword_Beschrijving,
					Keyword_BedragBij,
					Keyword_BedragAf,
					Keyword_Saldo
				];
			}
		}

		protected override bool CanParseRecords(List<SingleWordToken> words)
		{
			var foundTradeRepublic = false;
			var foundStatement = false;

			for (int i = 0; i < words.Count; i++)
			{
				if (IsCheckWords("Trade Republic Bank GmbH", words, i))
				{
					foundTradeRepublic = true;
				}

				if (
					IsCheckWords("MUTATIEOVERZICHT", words, i))
				{
					foundStatement = true;
				}
			}

			return foundTradeRepublic && foundStatement;
		}

		protected override List<PartialActivity> ParseRecords(List<SingleWordToken> words)
		{
			var activities = new List<PartialActivity>();
			var headers = DetectHeaders(words);
			
			if (headers.Count == TableKeyWords.Count)
			{
				ParseActivities(words, activities);
			}

			return activities;
		}

		private static List<MultiWordToken> DetectHeaders(List<SingleWordToken> words)
		{
			var headers = new List<MultiWordToken>();
			bool inHeader = false;
			int i = 0;

			while (i < words.Count)
			{
				var word = words[i];

				if (Keyword_Datum == word.Text)
				{
					inHeader = true;
				}

				if (inHeader)
				{
					var (Matched, Keyword, KeywordLength) = TryMatchKeyword(words, i);
					if (Matched)
					{
						headers.Add(new MultiWordToken(Keyword, word.BoundingBox));
						i += KeywordLength;
						continue;
					}
					else
					{
						inHeader = false;
						headers.Clear();
					}
				}

				if (Keyword_Saldo == word.Text)
				{
					inHeader = false;
				}

				i++;
			}

			return headers;
		}

		private static (bool Matched, string Keyword, int KeywordLength) TryMatchKeyword(List<SingleWordToken> words, int startIndex)
		{
			foreach (var keyword in TableKeyWords)
			{
				string[] keywordSplitted = keyword.Split(" ");
				bool keywordMatch = true;

				for (int j = 0; j < keywordSplitted.Length; j++)
				{
					if (startIndex + j >= words.Count || words[startIndex + j].Text != keywordSplitted[j])
					{
						keywordMatch = false;
						break;
					}
				}

				if (keywordMatch)
				{
					return (true, keyword, keywordSplitted.Length);
				}
			}

			return (false, string.Empty, 0);
		}

		private static void ParseActivities(List<SingleWordToken> words, List<PartialActivity> activities)
		{
			int i = 0;
			while (i < words.Count)
			{
				var incr = ParseActivity(words, i, activities);
				if (incr == int.MaxValue)
				{
					break;
				}

				i += incr;
			}
		}

		private static int ParseActivity(List<SingleWordToken> words, int i, List<PartialActivity> activities)
		{
			for (int j = i; j < words.Count - 2; j++)
			{
				var (Success, _) = TryParseDate(words, j);
				if (!Success)
					continue;

				var activityTypeToken = words[j + 3];
				if (ShouldSkipActivityType(activityTypeToken.Text))
				{
					return j - i + 3;
				}

				var activityData = ExtractActivityData(words, j, activityTypeToken);
				var activity = CreateActivityFromType(activityData);
				
				if (activity != null)
				{
					activities.Add(activity);
				}

				return j - i + 3 + activityData.Items.Count;
			}

			return int.MaxValue;
		}

		private static (bool Success, DateTime Date) TryParseDate(List<SingleWordToken> words, int startIndex)
		{
			var dutchCultureInfo = new CultureInfo("nl-NL");
			var dateText = $"{words[startIndex].Text} {words[startIndex + 1].Text} {words[startIndex + 2].Text}";
			
			if (DateTime.TryParseExact(
				dateText,
				["dd MMM yyyy", "dd MMM. yyyy"],
				dutchCultureInfo,
				DateTimeStyles.AssumeUniversal,
				out var date))
			{
				return (true, date);
			}

			return (false, default);
		}

		private static bool ShouldSkipActivityType(string activityType)
		{
			return activityType == "Handel" || activityType == "Inkomsten" || activityType == "Pagina";
		}

		private static ActivityData ExtractActivityData(List<SingleWordToken> words, int dateStartIndex, SingleWordToken activityTypeToken)
		{
			var items = words.Skip(dateStartIndex + 4)
				.TakeWhile(w => w.BoundingBox!.Row == activityTypeToken.BoundingBox!.Row)
				.ToList();

			var amountText = items[^2];
			var currency = Currency.GetCurrency(CurrencyTools.GetCurrencyFromSymbol(amountText.Text[..1]));
			var amount = decimal.Parse(amountText.Text[1..].Trim(), new CultureInfo("nl-NL"));
			var date = TryParseDate(words, dateStartIndex).Date;
			var id = $"Trade_Republic_{activityTypeToken.Text}_{date.ToInvariantDateOnlyString()}";
			System.Console.WriteLine(id);

			return new ActivityData
			{
				Date = date,
				ActivityType = activityTypeToken.Text,
				Currency = currency,
				Amount = amount,
				Items = items,
				TransactionId = id
			};
		}

		private static PartialActivity? CreateActivityFromType(ActivityData data)
		{
			return data.ActivityType switch
			{
				"Rentebetaling" => CreateInterestActivity(data),
				"Overschrijving" => CreateTransferActivity(data),
				"Kaarttransactie" => CreateCardTransactionActivity(data),
				"Beloning" => CreateRewardActivity(data),
				"Verwijzing" => CreateReferralActivity(data),
				"Handel" or "Inkomsten" => null, // Handled by other parsers
				_ => throw new NotSupportedException($"Activity type '{data.ActivityType}' is not supported")
			};
		}

		private static PartialActivity CreateInterestActivity(ActivityData data)
		{
			var description = string.Join(" ", data.Items.Take(data.Items.Count - 2));
			return PartialActivity.CreateInterest(
				data.Currency,
				data.Date,
				data.Amount,
				description,
				new Money(data.Currency, data.Amount),
				data.TransactionId);
		}

		private static PartialActivity CreateTransferActivity(ActivityData data)
		{
			var firstItemText = data.Items[0].Text;
			var itemTexts = data.Items.Select(x => x.Text).ToList();

			if (firstItemText == "PayOut")
			{
				return PartialActivity.CreateCashWithdrawal(
					data.Currency,
					data.Date,
					data.Amount,
					new Money(data.Currency, data.Amount),
					data.TransactionId);
			}

			if (IsDepositTransfer(firstItemText, itemTexts))
			{
				return PartialActivity.CreateCashDeposit(
					data.Currency,
					data.Date,
					data.Amount,
					new Money(data.Currency, data.Amount),
					data.TransactionId);
			}

			throw new NotSupportedException($"{firstItemText} not supported as an {data.ActivityType}");
		}

		private static bool IsDepositTransfer(string firstItemText, List<string> itemTexts)
		{
			return firstItemText == "Storting" ||
				   itemTexts.Contains("inpayed") ||
				   (itemTexts.Contains("Top") && itemTexts.Contains("up"));
		}

		private static PartialActivity CreateCardTransactionActivity(ActivityData data)
		{
			return PartialActivity.CreateCashWithdrawal(
				data.Currency,
				data.Date,
				data.Amount,
				new Money(data.Currency, data.Amount),
				data.TransactionId);
		}

		private static PartialActivity CreateRewardActivity(ActivityData data)
		{
			return PartialActivity.CreateGift(
				data.Currency,
				data.Date,
				data.Amount,
				new Money(data.Currency, data.Amount),
				data.TransactionId);
		}

		private static PartialActivity CreateReferralActivity(ActivityData data)
		{
			return PartialActivity.CreateGift(
				data.Currency,
				data.Date,
				data.Amount,
				new Money(data.Currency, data.Amount),
				data.TransactionId);
		}

		private sealed record ActivityData
		{
			public DateTime Date { get; init; }
			public string ActivityType { get; init; } = string.Empty;
			public Currency Currency { get; init; } = null!;
			public decimal Amount { get; init; }
			public List<SingleWordToken> Items { get; init; } = [];
			public string TransactionId { get; init; } = string.Empty;
		}
	}
}
