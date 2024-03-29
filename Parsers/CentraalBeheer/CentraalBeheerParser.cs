﻿using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using System.Globalization;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace GhostfolioSidekick.Parsers.CentraalBeheer
{
	public class CentraalBeheerParser : IFileImporter
	{
		private const string Keyword_Aankoop = "Aankoop";
		private const string Keyword_Verkoop = "Verkoop";
		private const string KeyWord_Overboeking = "Overboeking";
		private const string Keyword_Opdrachtdatum = "Opdrachtdatum";
		private const string Keyword_Aantal_Stukken = "Aantal stukken";
		private const string Keyword_Koers = "Koers";
		private const string Keyword_Aankoopkosten = "Aankoopkosten";
		private const string Keyword_Bruto_Bedrag = "Bruto bedrag";
		private readonly ICurrencyMapper currencyMapper;
		private readonly CultureInfo cultureInfo = new("nl-NL");

		private const string Prefix = "Centraal Beheer ";

		private List<string> mainKeyWords =
		[
			Keyword_Aankoop,
			Keyword_Verkoop,
			KeyWord_Overboeking
		];
		private List<string> subKeyWords =
		[
			Keyword_Opdrachtdatum,
			Keyword_Aantal_Stukken,
			Keyword_Koers,
			Keyword_Aankoopkosten,
			Keyword_Bruto_Bedrag
		];

		public CentraalBeheerParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		public Task<bool> CanParseActivities(string filename)
		{
			try
			{
				var records = ParseRecords(filename);
				return Task.FromResult(records.Any());
			}
			catch
			{
				return Task.FromResult(false);
			}
		}

		public Task ParseActivities(string filename, IHoldingsCollection holdingsAndAccountsCollection, string accountName)
		{
			var records = ParseRecords(filename);
			holdingsAndAccountsCollection.AddPartialActivity(accountName, records);

			return Task.CompletedTask;
		}

		private List<PartialActivity> ParseRecords(string filename)
		{
			List<PartialActivity> records;
			using (PdfDocument document = PdfDocument.Open(filename))
			{
				var singleWords = new List<SingleWordToken>();

				for (var i = 0; i < document.NumberOfPages; i++)
				{
					Page page = document.GetPage(i + 1);
					foreach (var word in page.GetWords())
					{
						singleWords.Add(new SingleWordToken(word.Text));
					}
				}

				var multiWords = new List<MultiWordToken>();
				MultiWordToken? currentMainMultiWord = null;
				MultiWordToken? currentMultiWord = null;
				for (int i = 0; i < singleWords.Count; i++)
				{
					var token = singleWords[i];

					var wasKeyword = false;
					foreach (var keyWord in mainKeyWords.Union(subKeyWords))
					{
						var spaces = keyWord.Count(c => c == ' ');

						if (i + spaces + 1 > singleWords.Count)
						{
							continue;
						}

						var tokenOfCorrectSize = string.Join(' ', singleWords.GetRange(i, spaces + 1).Select(x => x.Text));
						var isMatch = tokenOfCorrectSize.Equals(keyWord);

						bool isMainLevel = mainKeyWords.Contains(keyWord);
						if (isMatch && isMainLevel)
						{
							currentMultiWord = currentMainMultiWord = new MultiWordToken(keyWord);
							multiWords.Add(currentMultiWord);
							i += spaces;
							wasKeyword = true;
						}
						else if (isMatch)
						{
							var subWord = new MultiWordToken(keyWord);
							currentMainMultiWord!.AddMultiWord(subWord);
							currentMultiWord = subWord;
							i += spaces;
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
				decimal.Parse(GetToken(Keyword_Aantal_Stukken, relevantTokens).First(), cultureInfo),
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
				decimal.Parse(GetToken(Keyword_Aantal_Stukken, relevantTokens).First(), cultureInfo),
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

		public static class CurrencyTools
		{
			private static IDictionary<string, string> map;
			static CurrencyTools()
			{
				map = CultureInfo
					.GetCultures(CultureTypes.AllCultures)
					.Where(c => !c.IsNeutralCulture)
					.Select(culture =>
					{
						try
						{
							return new RegionInfo(culture.Name);
						}
						catch
						{
							return null;
						}
					})
					.Where(ri => ri != null)
					.GroupBy(ri => ri!.ISOCurrencySymbol)
					.ToDictionary(x => x.Key, x => x.First()!.CurrencySymbol);
			}

			public static bool TryGetCurrencySymbol(
								  string ISOCurrencySymbol,
								  out string? symbol)
			{
				return map.TryGetValue(ISOCurrencySymbol, out symbol);
			}

			public static string GetCurrencyFromSymbol(string currencySymbol)
			{
				var isoCurrencySymbol = map
					.Where(kvp => kvp.Value == currencySymbol)
					.Select(kvp => kvp.Key)
					.FirstOrDefault();

				if (isoCurrencySymbol == null)
				{
					throw new ArgumentException("Currency symbol not found");
				}

				return isoCurrencySymbol;
			}
		}
	}
}
