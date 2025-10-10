using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Logging;
using YahooFinanceApi;

namespace GhostfolioSidekick.ExternalDataProvider.Yahoo
{
	public class YahooRepository(ILogger<YahooRepository> logger) :
		ICurrencyRepository,
		ISymbolMatcher,
		IStockPriceRepository,
		IStockSplitRepository
	{
		public string DataSource => Datasource.YAHOO;

		public DateOnly MinDate => DateOnly.MinValue;

		public async Task<IEnumerable<CurrencyExchangeRate>> GetCurrencyHistory(Currency currencyFrom, Currency currencyTo, DateOnly fromDate)
		{
			var marketData = await GetStockMarketData($"{currencyFrom.Symbol.ToUpperInvariant()}{currencyTo.Symbol.ToUpperInvariant()}=X", currencyFrom, fromDate);

			var result = marketData.Select(x => new CurrencyExchangeRate
			{
				Date = x.Date,
				Close = x.Close,
				High = x.High,
				Low = x.Low,
				Open = x.Open,
				TradingVolume = x.TradingVolume,
			}).ToList();

			return result;
		}

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			var searchResults = await GetSearchResultsForIdentifiers(symbolIdentifiers);
			
			if (searchResults.Count == 0)
			{
				return null;
			}

			var bestMatch = searchResults.First();

			return await CreateSymbolProfileFromMatch(bestMatch);
		}

		private async Task<List<SearchResult>> GetSearchResultsForIdentifiers(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			var matches = new List<SearchResult>();
			
			foreach (var identifier in symbolIdentifiers)
			{
				if (string.IsNullOrWhiteSpace(identifier.Identifier))
				{
					continue;
				}

				var searchTerm = PrepareSearchTerm(identifier);
				var results = await SearchSymbol(searchTerm);
				
				if (results != null)
				{
					var filteredResults = results.Where(result => IsAllowedSymbolType(result, identifier));
					matches.AddRange(filteredResults);
				}
			}

			return matches;
		}

		private static string PrepareSearchTerm(PartialSymbolIdentifier identifier)
		{
			var searchTerm = identifier.Identifier;

			if (identifier.AllowedAssetSubClasses?.Contains(AssetSubClass.CryptoCurrency) ?? false)
			{
				searchTerm = $"{searchTerm}-USD";
			}

			return searchTerm;
		}

		private async Task<SearchResult[]?> SearchSymbol(string searchTerm)
		{
			return await RetryPolicyHelper
				.GetFallbackPolicy<SearchResult[]>(logger)
				.WrapAsync(RetryPolicyHelper.GetRetryPolicy(logger))
				.ExecuteAsync(() => YahooFinanceApi.Yahoo.SearchAsync(searchTerm));
		}

		private async Task<SymbolProfile?> CreateSymbolProfileFromMatch(SearchResult match)
		{
			var symbols = await GetSymbolDetails(match.Symbol);
			if (symbols == null)
			{
				return null;
			}

			var symbol = symbols.OrderBy(x => x.Value.Symbol == match.Symbol).First().Value;
			if (symbol == null)
			{
				return null;
			}

			var securityProfile = await GetSecurityProfile(symbol.Symbol);

			return new SymbolProfile(
				symbol.Symbol,
				GetName(symbol),
				[symbol.Symbol, GetName(symbol)],
				Currency.GetCurrency(symbol.Currency),
				Datasource.YAHOO,
				ParseQuoteType(symbol.QuoteType),
				ParseQuoteTypeAsSub(symbol.QuoteType),
				GetCountries(securityProfile),
				GetSectors(securityProfile));
		}

		private async Task<IReadOnlyDictionary<string, Security>?> GetSymbolDetails(string symbolName)
		{
			return await RetryPolicyHelper
				.GetFallbackPolicy<IReadOnlyDictionary<string, Security>>(logger)
				.WrapAsync(RetryPolicyHelper.GetRetryPolicy(logger))
				.ExecuteAsync(() => YahooFinanceApi.Yahoo.Symbols(symbolName).QueryAsync());
		}

		private async Task<SecurityProfile?> GetSecurityProfile(string symbolName)
		{
			return await RetryPolicyHelper
				.GetFallbackPolicy<SecurityProfile>(logger)
				.WrapAsync(RetryPolicyHelper.GetRetryPolicy(logger))
				.ExecuteAsync(() => YahooFinanceApi.Yahoo.QueryProfileAsync(symbolName));
		}

		private static bool IsAllowedSymbolType(SearchResult searchResult, PartialSymbolIdentifier identifier)
		{
			if (identifier.AllowedAssetClasses == null)
			{
				return true;
			}

			if (!identifier.AllowedAssetClasses.Contains(ParseQuoteType(searchResult.Type)))
			{
				return false;
			}

			if (identifier.AllowedAssetSubClasses == null)
			{
				return true;
			}

			var assetSubClass = ParseQuoteTypeAsSub(searchResult.Type);
			return assetSubClass == null || identifier.AllowedAssetSubClasses.Contains(assetSubClass.Value);
		}

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
		{
			var history = await RetryPolicyHelper.GetFallbackPolicy<IReadOnlyList<Candle>>(logger)
					.WrapAsync(RetryPolicyHelper.GetRetryPolicy(logger))
					.ExecuteAsync(() =>
								YahooFinanceApi
										.Yahoo
										.GetHistoricalAsync(symbol.Symbol, new DateTime(fromDate, TimeOnly.MinValue, DateTimeKind.Utc), null, Period.Daily)
					);

			if (history == null)
			{
				return [];
			}

			var list = new List<MarketData>();
			foreach (var candle in history)
			{
				var item = new MarketData(
									new Money(symbol.Currency with { }, candle.Close),
									new Money(symbol.Currency with { }, candle.Open),
									new Money(symbol.Currency with { }, candle.High),
									new Money(symbol.Currency with { }, candle.Low),
									candle.Volume,
									DateOnly.FromDateTime(candle.DateTime.Date));
				list.Add(item);
			}

			return list;
		}

		public async Task<IEnumerable<StockSplit>> GetStockSplits(SymbolProfile symbol, DateOnly fromDate)
		{
			var list = new List<StockSplit>();

			try
			{
				var history = await YahooFinanceApi.Yahoo.GetSplitsAsync(symbol.Symbol, new DateTime(fromDate, TimeOnly.MinValue, DateTimeKind.Utc), DateTime.Today);

				foreach (var candle in history)
				{
					var item = new StockSplit(Date: DateOnly.FromDateTime(candle.DateTime), BeforeSplit: candle.AfterSplit, AfterSplit: candle.BeforeSplit); // API has them mixed up
					list.Add(item);
				}

			}
			catch (RuntimeBinderException ex) when (ex.Message.Contains("'System.Dynamic.ExpandoObject'"))
			{
				// No data?
			}

			return list;
		}

		private async Task<IEnumerable<MarketData>> GetStockMarketData(string symbol, Currency currency, DateOnly fromDate)
		{
			var history = await RetryPolicyHelper.GetFallbackPolicy<IReadOnlyList<Candle>>(logger)
					.WrapAsync(RetryPolicyHelper.GetRetryPolicy(logger))
					.ExecuteAsync(() => YahooFinanceApi.Yahoo.GetHistoricalAsync(symbol, new DateTime(fromDate, TimeOnly.MinValue, DateTimeKind.Utc), null, Period.Daily));

			if (history == null)
			{
				return [];
			}

			var list = new List<MarketData>();
			foreach (var candle in history)
			{
				var item = new MarketData(
									new Money(currency with { }, candle.Close),
									new Money(currency with { }, candle.Open),
									new Money(currency with { }, candle.High),
									new Money(currency with { }, candle.Low),
									candle.Volume,
									DateOnly.FromDateTime(candle.DateTime.Date));
				list.Add(item);
			}

			return list;
		}

		private static string? GetName(Security security)
		{
			if (security is null)
			{
				return null;
			}

			if (security.Fields.ContainsKey(Field.LongName.ToString()))
			{
				return security.LongName;
			}

			if (security.Fields.ContainsKey(Field.ShortName.ToString()))
			{
				return security.ShortName;
			}

			return security.Symbol;
		}

		private static SectorWeight[] GetSectors(SecurityProfile? securityProfile)
		{
			if (securityProfile is null)
			{
				return [];
			}

			if (!securityProfile.Fields.ContainsKey(ProfileFields.Sector.ToString()))
			{
				return [];
			}

			return [new SectorWeight(securityProfile.Sector, 1)];
		}

		private static CountryWeight[] GetCountries(SecurityProfile? securityProfile)
		{
			if (securityProfile is null)
			{
				return [];
			}

			if (!securityProfile.Fields.ContainsKey(ProfileFields.Country.ToString()))
			{
				return [];
			}

			return [new CountryWeight(securityProfile.Country, string.Empty, string.Empty, 1)];
		}

		private static AssetClass ParseQuoteType(string quoteType)
		{
			return quoteType switch
			{
				"EQUITY" => AssetClass.Equity,
				"ETF" => AssetClass.Equity,
				"MUTUALFUND" => AssetClass.Undefined,
				"CURRENCY" or "CRYPTOCURRENCY" => AssetClass.Liquidity,
				"INDEX" => AssetClass.Undefined,
				_ => AssetClass.Undefined,
			};
		}

		private static AssetSubClass? ParseQuoteTypeAsSub(string quoteType)
		{
			return quoteType switch
			{
				"EQUITY" => (AssetSubClass?)AssetSubClass.Stock,
				"ETF" => (AssetSubClass?)AssetSubClass.Etf,
				"CRYPTOCURRENCY" => (AssetSubClass?)AssetSubClass.CryptoCurrency,
				"CURRENCY" => (AssetSubClass?)AssetSubClass.Undefined,
				"MUTUALFUND" => (AssetSubClass?)AssetSubClass.Undefined,
				_ => null,
			};
		}
	}
}
