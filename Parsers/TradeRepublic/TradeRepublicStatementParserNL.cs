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

				if (IsCheckWords("MUTATIEOVERZICHT", words, i))
				{
					foundStatement = true;
				}
			}

			return foundTradeRepublic && foundStatement;
		}

		protected override List<PartialActivity> ParseRecords(List<SingleWordToken> words)
		{
			var activities = new List<PartialActivity>();
			var rows = PdfTableExtractor.FindTableRows(words, TableKeyWords.ToArray());

			foreach (var row in rows)
			{
				var tokens = row.Tokens;
				if (tokens.Count < 5)
				{
					continue;
				}

				if (!TryParseDate(tokens, 0, out var date))
				{
					continue;
				}

				if (tokens.Count < 4)
				{
					continue;
				}

				var typeToken = tokens[3];
				if (ShouldSkipActivityType(typeToken.Text))
				{
					continue;
				}

				var items = tokens.Skip(4).ToList();
				if (items.Count < 2)
				{
					continue;
				}

				var (currency, amount) = ParseAmountAndCurrency(items);
				var id = GenerateTransactionId(typeToken.Text, date);

				CreateActivityBasedOnType(typeToken.Text, date, currency, amount, items, activities, id);
			}

			return activities;
		}

		private static bool TryParseDate(IReadOnlyList<SingleWordToken> tokens, int index, out DateTime date)
		{
			date = default;
			if (index + 2 >= tokens.Count)
			{
				return false;
			}

			var dateString = $"{tokens[index].Text} {tokens[index + 1].Text} {tokens[index + 2].Text}";
			return DateTime.TryParseExact(
				dateString,
				["dd MMM yyyy", "dd MMM. yyyy"],
				DutchCultureInfo,
				DateTimeStyles.AssumeUniversal,
				out date);
		}

		private static bool ShouldSkipActivityType(string activityType)
		{
			return activityType is "Handel" or "Inkomsten" or "Pagina";
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
	}
}
