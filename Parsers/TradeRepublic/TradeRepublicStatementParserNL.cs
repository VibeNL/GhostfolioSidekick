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

		private static readonly CultureInfo DutchCultureInfo = new("nl-NL");

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
			var headers = new List<MultiWordToken>();

			var parsingState = new ParsingState();

			int i = 0;
			while (i < words.Count)
			{
				if (IsReadyToParseActivities(headers))
				{
					var increment = ParseActivity(words, i, activities);
					if (increment == int.MaxValue)
					{
						break;
					}
					i += increment;
				}
				else
				{
					var newIndex = ProcessHeaderParsing(words, i, headers, parsingState);
					i = newIndex + 1; // Move to next word after processing current one
				}
			}

			return activities;
		}

		private static bool IsReadyToParseActivities(List<MultiWordToken> headers)
		{
			return headers.Count == TableKeyWords.Count;
		}

		private static int ProcessHeaderParsing(List<SingleWordToken> words, int currentIndex, List<MultiWordToken> headers, ParsingState state)
		{
			var word = words[currentIndex];

			if (IsStartOfHeader(word.Text))
			{
				state.InHeader = true;
			}

			if (state.InHeader)
			{
				var headerProcessResult = ProcessHeaderKeywords(words, currentIndex, headers);
				if (headerProcessResult.Found)
				{
					return currentIndex + headerProcessResult.Increment;
				}
				else
				{
					ResetHeaderParsing(headers, state);
				}
			}

			if (IsEndOfHeader(word.Text))
			{
				state.InHeader = false;
			}

			return currentIndex;
		}

		private static bool IsStartOfHeader(string text)
		{
			return text == Keyword_Datum;
		}

		private static bool IsEndOfHeader(string text)
		{
			return text == Keyword_Saldo;
		}

		private static HeaderProcessResult ProcessHeaderKeywords(List<SingleWordToken> words, int currentIndex, List<MultiWordToken> headers)
		{
			var word = words[currentIndex];

			foreach (var keyword in TableKeyWords)
			{
				var matchResult = TryMatchKeyword(words, currentIndex, keyword);
				if (matchResult.IsMatch)
				{
					headers.Add(new MultiWordToken(keyword, word.BoundingBox));
					return new HeaderProcessResult(true, matchResult.WordCount - 1);
				}
			}

			return new HeaderProcessResult(false, 0);
		}

		private static KeywordMatchResult TryMatchKeyword(List<SingleWordToken> words, int startIndex, string keyword)
		{
			var keywordParts = keyword.Split(" ");

			for (int i = 0; i < keywordParts.Length; i++)
			{
				if (startIndex + i >= words.Count || words[startIndex + i].Text != keywordParts[i])
				{
					return new KeywordMatchResult(false, 0);
				}
			}

			return new KeywordMatchResult(true, keywordParts.Length);
		}

		private static void ResetHeaderParsing(List<MultiWordToken> headers, ParsingState state)
		{
			state.InHeader = false;
			headers.Clear();
		}

		private static int ParseActivity(List<SingleWordToken> words, int i, List<PartialActivity> activities)
		{
			for (int j = i; j < words.Count - 2; j++)
			{
				if (TryParseDate(words, j, out var date))
				{
					return ProcessActivityAtDate(words, j, date, activities, i);
				}
			}

			return int.MaxValue;
		}

		private static bool TryParseDate(List<SingleWordToken> words, int index, out DateTime date)
		{
			date = default;

			if (index + 2 >= words.Count)
				return false;

			var dateString = $"{words[index].Text} {words[index + 1].Text} {words[index + 2].Text}";

			return DateTime.TryParseExact(
				dateString,
				["dd MMM yyyy", "dd MMM. yyyy"],
				DutchCultureInfo,
				DateTimeStyles.AssumeUniversal,
				out date);
		}

		private static int ProcessActivityAtDate(List<SingleWordToken> words, int dateIndex, DateTime date, List<PartialActivity> activities, int originalIndex)
		{
			var typeTokenIndex = dateIndex + 3;
			if (typeTokenIndex >= words.Count)
				return int.MaxValue;

			var typeToken = words[typeTokenIndex];

			if (ShouldSkipActivityType(typeToken.Text))
			{
				return dateIndex - originalIndex + 3;
			}

			var items = GetActivityItems(words, typeTokenIndex);
			var (currency, amount) = ParseAmountAndCurrency(items);
			var id = GenerateTransactionId(typeToken.Text, date);

			CreateActivityBasedOnType(typeToken.Text, date, currency, amount, items, activities, id);

			return dateIndex - originalIndex + 3 + items.Count;
		}

		private static bool ShouldSkipActivityType(string activityType)
		{
			return activityType is "Handel" or "Inkomsten" or "Pagina";
		}

		private static List<SingleWordToken> GetActivityItems(List<SingleWordToken> words, int typeTokenIndex)
		{
			var typeToken = words[typeTokenIndex];
			return [.. words.Skip(typeTokenIndex + 1).TakeWhile(w => w.BoundingBox!.Row == typeToken.BoundingBox!.Row)];
		}

		private static (Currency currency, decimal amount) ParseAmountAndCurrency(List<SingleWordToken> items)
		{
			var amountText = items[^2];
			var currency = Currency.GetCurrency(CurrencyTools.GetCurrencyFromSymbol(amountText.Text[..1]));
			var amount = decimal.Parse(amountText.Text[1..].Trim(), DutchCultureInfo);

			return (currency, amount);
		}

		private static string GenerateTransactionId(string activityType, DateTime date)
		{
			return $"Trade_Republic_{activityType}_{date.ToInvariantDateOnlyString()}";
		}

		private static void CreateActivityBasedOnType(
			string activityType,
			DateTime date,
			Currency currency,
			decimal amount,
			List<SingleWordToken> items,
			List<PartialActivity> activities,
			string id)
		{
			switch (activityType)
			{
				case "Rentebetaling":
					CreateInterestActivity(currency, date, amount, items, activities, id);
					break;
				case "Overschrijving":
					CreateTransferActivity(currency, date, amount, items, activities, id);
					break;
				case "Kaarttransactie":
					CreateCardTransactionActivity(currency, date, amount, activities, id);
					break;
				case "Beloning":
				case "Verwijzing":
					CreateGiftActivity(currency, date, amount, activities, id);
					break;
				case "Handel":
				case "Inkomsten":
					// These are handled by other parsers
					break;
				default:
					throw new NotSupportedException($"Activity type '{activityType}' is not supported");
			}
		}

		private static void CreateInterestActivity(Currency currency, DateTime date, decimal amount, List<SingleWordToken> items, List<PartialActivity> activities, string id)
		{
			var description = string.Join(" ", items.Take(items.Count - 2));
			activities.Add(PartialActivity.CreateInterest(
				currency,
				date,
				amount,
				description,
				new Money(currency, amount),
				id));
		}

		private static void CreateTransferActivity(Currency currency, DateTime date, decimal amount, List<SingleWordToken> items, List<PartialActivity> activities, string id)
		{
			var firstItemText = items[0].Text;
			var itemTexts = items.Select(x => x.Text).ToList();

			if (IsPayOut(firstItemText))
			{
				activities.Add(PartialActivity.CreateCashWithdrawal(
					currency,
					date,
					amount,
					new Money(currency, amount),
					id));
			}
			else if (IsDeposit(firstItemText, itemTexts))
			{
				activities.Add(PartialActivity.CreateCashDeposit(
					currency,
					date,
					amount,
					new Money(currency, amount),
					id));
			}
			else
			{
				throw new NotSupportedException($"{firstItemText} not supported as an Overschrijving");
			}
		}

		private static bool IsPayOut(string firstItemText)
		{
			return firstItemText == "PayOut";
		}

		private static bool IsDeposit(string firstItemText, List<string> itemTexts)
		{
			return firstItemText == "Storting" ||
				   itemTexts.Contains("inpayed") ||
				   (itemTexts.Contains("Top") && itemTexts.Contains("up"));
		}

		private static void CreateCardTransactionActivity(Currency currency, DateTime date, decimal amount, List<PartialActivity> activities, string id)
		{
			activities.Add(PartialActivity.CreateCashWithdrawal(
				currency,
				date,
				amount,
				new Money(currency, amount),
				id));
		}

		private static void CreateGiftActivity(Currency currency, DateTime date, decimal amount, List<PartialActivity> activities, string id)
		{
			activities.Add(PartialActivity.CreateGift(
				currency,
				date,
				amount,
				new Money(currency, amount),
				id));
		}

		// Helper classes for parsing state management
		private sealed class ParsingState
		{
			public bool InHeader { get; set; }
		}

		private sealed record HeaderProcessResult(bool Found, int Increment);
		private sealed record KeywordMatchResult(bool IsMatch, int WordCount);
	}
}
