using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using System.Linq;
using YahooFinanceApi;

namespace GhostfolioSidekick.ExternalDataProvider.Yahoo
{
	public class YahooRepository(ILogger<YahooRepository> logger) :
		ICurrencyRepository,
		ISymbolMatcher
	{
		public async Task<IEnumerable<MarketData>> GetCurrencyHistory(Currency currencyFrom, Currency currencyTo, DateOnly fromDate)
		{
			var history = await YahooFinanceApi.Yahoo.GetHistoricalAsync($"{currencyFrom.Symbol.ToUpperInvariant()}{currencyTo.Symbol.ToUpperInvariant()}=X", new DateTime(fromDate, TimeOnly.MinValue), DateTime.Today, Period.Daily);

			var list = new List<MarketData>();
			foreach (var candle in history)
			{
				MarketData item = new MarketData(
									new Money(currencyTo with { }, candle.Close),
									new Money(currencyTo with { }, candle.Open),
									new Money(currencyTo with { }, candle.High),
									new Money(currencyTo with { }, candle.Low),
									candle.Volume,
									candle.DateTime.Date);
				list.Add(item);
			}

			return list;
		}

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] identifiers)
		{
			var matches = new List<SearchResult>();
			foreach (var id in identifiers)
			{
				var searchResults = await YahooFinanceApi.Yahoo.FindProfileAsync(id.Identifier);
				if (searchResults != null)
				{
					matches.AddRange((IEnumerable<SearchResult>)searchResults.Where(x => FilterOnAllowedType(x, id)));
				}
			}

			if (matches.Count == 0)
			{
				return null;
			}

			// Get the best match of the correct QuoteType
			var bestMatch = matches.OrderByDescending(x => x.Score).First();

			var symbols = await YahooFinanceApi.Yahoo.Symbols(bestMatch.Symbol).QueryAsync();
			var symbol = symbols.OrderBy(x => x.Value.Symbol == bestMatch.Symbol).First().Value;

			var securityProfile = await YahooFinanceApi.Yahoo.QueryProfileAsync(symbol.Symbol);

			return new SymbolProfile(symbol.Symbol, symbol.LongName, [], new Currency(symbol.Currency), "YAHOO", ParseQuoteType(symbol.QuoteType), ParseQuoteTypeAsSub(symbol.QuoteType), GetCountries(securityProfile), GetSectors(securityProfile));

			bool FilterOnAllowedType(SearchResult x, PartialSymbolIdentifier id)
			{
				if (id.AllowedAssetClasses == null)
				{
					return true;
				}

				if (!id.AllowedAssetClasses.Contains(ParseQuoteType(x.QuoteType))){
					return false;
				}

				if (id.AllowedAssetSubClasses == null)
				{
					return true;
				}

				return ParseQuoteTypeAsSub(x.QuoteType) == null || id.AllowedAssetSubClasses.Contains(ParseQuoteTypeAsSub(x.QuoteType).Value);
			}
		}

		private SectorWeight[] GetSectors(SecurityProfile securityProfile)
		{
			return [new SectorWeight(securityProfile.Sector, 1)];
		}

		private CountryWeight[] GetCountries(SecurityProfile securityProfile)
		{
			return [new CountryWeight(securityProfile.Country, string.Empty, string.Empty, 1)];
		}

		private AssetClass ParseQuoteType(string quoteType)
		{
			switch (quoteType)
			{
				case "EQUITY":
					return AssetClass.Equity;
				case "ETF":
					return AssetClass.Equity;
				case "MUTUALFUND":
					return AssetClass.Undefined;
				default: 
					return AssetClass.Undefined;
			};
		}

		private AssetSubClass? ParseQuoteTypeAsSub(string quoteType)
		{
			switch (quoteType)
			{
				case "EQUITY":
					return AssetSubClass.Stock;
				case "ETF":
					return AssetSubClass.Etf;
				default:
					return null;
			};
		}

	}
}
