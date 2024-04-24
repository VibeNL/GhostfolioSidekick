using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using System.Globalization;
using Spire.Pdf.Texts;
using Spire.Pdf;

namespace GhostfolioSidekick.Parsers.CentraalBeheer
{
	public partial class CentraalBeheerParser : PdfBaseParser, IFileImporter
	{
		private const string Keyword_Aankoop = "Aankoop";
		private const string Keyword_Verkoop = "Verkoop";
		private const string KeyWord_Overboeking = "Overboeking";
		private const string Keyword_Opdrachtdatum = "Opdrachtdatum";
		private const string Keyword_Aantal_Stukken = "Aantal stukken";
		private const string Keyword_Koers = "Koers";
		private const string Keyword_Aankoopkosten = "Aankoopkosten";
		private const string Keyword_Bruto_Bedrag = "Bruto bedrag";
		private readonly CultureInfo cultureInfo = new("nl-NL");

		private const string Prefix = "Centraal Beheer ";

		private List<string> MainKeyWords
		{
			get
			{
				return [
					Keyword_Aankoop,
					Keyword_Verkoop,
					KeyWord_Overboeking
				];
			}
		}

		private List<string> SubKeyWords
		{
			get
			{
				return [
					Keyword_Opdrachtdatum,
			Keyword_Aantal_Stukken,
			Keyword_Koers,
			Keyword_Aankoopkosten,
			Keyword_Bruto_Bedrag
				];
			}
		}

		protected override List<PartialActivity> ParseRecords(string filename)
		{
			List<PartialActivity> records;

			using (PdfDocument document = new PdfDocument())
			{
				// Load a PDF file
				document.LoadFromFile(filename);

				var singleWords = new List<SingleWordToken>();

				foreach (PdfPageBase page in document.Pages)
				{
					PdfTextExtractor textExtractor = new PdfTextExtractor(page);
					var text = textExtractor.ExtractText(new PdfTextExtractOptions());

					foreach (var word in text.Split(" ").Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
					{
						singleWords.Add(new SingleWordToken(word));
					}
				}

				var multiWords = new List<MultiWordToken>();
				MultiWordToken? currentMainMultiWord = null;
				MultiWordToken? currentMultiWord = null;
				for (int i = 0; i < singleWords.Count; i++)
				{
					var token = singleWords[i];

					var wasKeyword = false;
					foreach (var keyWord in MainKeyWords.Union(SubKeyWords))
					{
						var spaces = keyWord.Count(c => c == ' ');

						if (i + spaces + 1 > singleWords.Count)
						{
							continue;
						}

						var tokenOfCorrectSize = string.Join(' ', singleWords.GetRange(i, spaces + 1).Select(x => x.Text));
						var isMatch = tokenOfCorrectSize.Equals(keyWord);

						bool isMainLevel = MainKeyWords.Contains(keyWord);
						if (isMatch && isMainLevel)
						{
							currentMultiWord = currentMainMultiWord = new MultiWordToken(keyWord);
							multiWords.Add(currentMultiWord);
#pragma warning disable S127 // "for" loop stop conditions should be invariant
							i += spaces;
#pragma warning restore S127 // "for" loop stop conditions should be invariant
							wasKeyword = true;
						}
						else if (isMatch)
						{
							var subWord = new MultiWordToken(keyWord);
							currentMainMultiWord!.AddMultiWord(subWord);
							currentMultiWord = subWord;
#pragma warning disable S127 // "for" loop stop conditions should be invariant
							i += spaces;
#pragma warning restore S127 // "for" loop stop conditions should be invariant
							wasKeyword = true;
						}
					}

					if (currentMultiWord != null && !wasKeyword)
					{
						currentMultiWord.AddSingleWordToken(token);
					}
				}

				records = ParseTokens(multiWords);
			}

			return records;
		}

		private List<PartialActivity> ParseTokens(List<MultiWordToken> tokens)
		{
			var records = new List<PartialActivity>();

			for (int i = 0; i < tokens.Count; i++)
			{
				var token = tokens[i];

				switch (token)
				{
					case var t when t.KeyWord == Keyword_Aankoop:
						records.AddRange(CreateAankoopActivities(t.Words));
						break;
					case var t when t.KeyWord == Keyword_Verkoop:
						records.AddRange(CreateVerkoopActivities(t.Words));
						break;
					case var t when t.KeyWord == KeyWord_Overboeking:
						records.AddRange(CreateOverboekingActivities(t.Words));
						break;
					default:
						break;
				}
			}

			return records;
		}

		private IEnumerable<PartialActivity> CreateAankoopActivities(List<WordToken> relevantTokens)
		{
			var price = GetMoney(GetToken(Keyword_Koers, relevantTokens));
			var date = GetDate(GetToken(Keyword_Opdrachtdatum, relevantTokens));
			var symbol = Prefix + string.Join(" ", relevantTokens.OfType<SingleWordToken>().Skip(2).Select(x => x.Text)); // skip price

			var id = $"Centraal_Beheer_{PartialActivityType.Buy}_{symbol}_{date.ToInvariantDateOnlyString()}";

			yield return PartialActivity.CreateBuy(
				price.Currency,
				date,
				[PartialSymbolIdentifier.CreateStockAndETF(symbol)],
				decimal.Parse(GetToken(Keyword_Aantal_Stukken, relevantTokens)[0], cultureInfo),
				price.Amount,
				GetMoney(GetToken(Keyword_Bruto_Bedrag, relevantTokens)),
				id);

			var feeToken = GetToken(Keyword_Aankoopkosten, relevantTokens);
			if (feeToken.Any())
			{
				Money fee = GetMoney(feeToken);
				yield return PartialActivity.CreateFee(
					fee.Currency,
					date,
					fee.Amount,
					new Money(price.Currency, 0),
					id);
			}
		}

		private IEnumerable<PartialActivity> CreateVerkoopActivities(List<WordToken> relevantTokens)
		{
			var price = GetMoney(GetToken(Keyword_Koers, relevantTokens));
			var date = GetDate(GetToken(Keyword_Opdrachtdatum, relevantTokens));
			var symbol = Prefix + string.Join(" ", relevantTokens.OfType<SingleWordToken>().Skip(3).Select(x => x.Text)); // skip price

			var id = $"Centraal_Beheer_{PartialActivityType.Buy}_{symbol}_{date.ToInvariantDateOnlyString()}";

			yield return PartialActivity.CreateSell(
				price.Currency,
				date,
				[PartialSymbolIdentifier.CreateStockAndETF(symbol)],
				decimal.Parse(GetToken(Keyword_Aantal_Stukken, relevantTokens)[0], cultureInfo),
				price.Amount,
				GetMoney(GetToken(Keyword_Bruto_Bedrag, relevantTokens)),
				id);
		}

		private IEnumerable<PartialActivity> CreateOverboekingActivities(List<WordToken> relevantTokens)
		{
			var date = GetDate(GetToken(Keyword_Opdrachtdatum, relevantTokens));
			var amount = GetMoney(GetToken(Keyword_Bruto_Bedrag, relevantTokens));

			yield return PartialActivity.CreateCashDeposit(
				amount.Currency,
				date,
				amount.Amount,
				amount,
				$"Centraal_Beheer_{PartialActivityType.CashDeposit}_{date.ToInvariantDateOnlyString()}");
		}

		private string[] GetToken(string keyword, List<WordToken> relevantTokens)
		{
			return relevantTokens
				.OfType<MultiWordToken>()
				.Where(t => t.KeyWord == keyword)
				.SelectMany(t => t.Words)
				.OfType<SingleWordToken>()
				.Select(t => t.Text)
				.ToArray();
		}

		private DateTime GetDate(params string[] date)
		{
			if (!DateTime.TryParse(string.Join(" ", date), cultureInfo, DateTimeStyles.AssumeUniversal, out DateTime parsedDate))
			{
				throw new ArgumentException("Invalid date format");
			}

			return parsedDate;
		}

		private Money GetMoney(params string[] tokens)
		{
			if (!decimal.TryParse(tokens[1], cultureInfo, out decimal parsedAmount))
			{
				throw new ArgumentException("Invalid amount format");
			}

			return new Money(new Currency(CurrencyTools.GetCurrencyFromSymbol(tokens[0])), parsedAmount);
		}
	}
}
