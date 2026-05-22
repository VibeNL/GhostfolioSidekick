using GhostfolioSidekick.ExternalDataProvider.Cache;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Utilities;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Logging;
using Polly.Retry;
using YahooFinanceApi;

namespace GhostfolioSidekick.ExternalDataProvider.Yahoo
{
	public class YahooRepository(ILogger<YahooRepository> logger, IExternalDataCacheService cacheService) :
			ICurrencyRepository,
			ISymbolMatcher,
			IStockPriceRepository,
			IStockSplitRepository
	{
		public string DataSource => Datasource.YAHOO;

		public DateOnly MinDate => DateOnly.MinValue;

		public bool AllowedForDeterminingHolding => true;

		public async Task<IEnumerable<CurrencyExchangeRate>> GetCurrencyHistory(Currency currencyFrom, Currency currencyTo, DateOnly fromDate)
		{
			IEnumerable<MarketData> marketData = await GetStockMarketData($"{currencyFrom.Symbol.ToUpperInvariant()}{currencyTo.Symbol.ToUpperInvariant()}=X", currencyFrom, fromDate);

			var result = marketData.Select(x => new CurrencyExchangeRate
			{
				Date = x.Date,
				Currency = x.Currency,
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
			PartialSymbolIdentifier? id = symbolIdentifiers.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Identifier));
			if (id == null)
			{
				return null;
			}

			string cacheKey = $"{id.Identifier}";
			return await cacheService.GetOrAddAsync<SymbolProfile>(Source.Yahoo, TypeOfData.SymbolProfile, cacheKey, async () =>
			{
				AsyncRetryPolicy retryPolicy = RetryPolicyHelper.GetRetryPolicy(logger);
				return (await retryPolicy.ExecuteAsync(async () =>
				{
					List<SearchResult> searchResults = await GetSearchResultsForIdentifiers(symbolIdentifiers);
					if (searchResults.Count == 0)
					{
						return null;
					}

					SearchResult? bestMatch = null;
					Currency? expectedCurrency = symbolIdentifiers
						.Where(i => i.Currency != null)
						.Select(i => i.Currency)
						.FirstOrDefault();
					var symbolCurrencies = new Dictionary<string, Currency?>(StringComparer.OrdinalIgnoreCase);
					foreach (SearchResult result in searchResults)
					{
						symbolCurrencies[result.Symbol] = await GetActualCurrencyAsync(result.Symbol);
					}
					foreach (PartialSymbolIdentifier identifier in symbolIdentifiers)
					{
						var cleanedIdentifier = SymbolNameCleaner.CleanTickerSymbol(identifier.Identifier);
						var exactMatches = searchResults
							.Where(r => r.Symbol.Equals(identifier.Identifier, StringComparison.OrdinalIgnoreCase)
								 || SymbolNameCleaner.CleanTickerSymbol(r.Symbol).Equals(cleanedIdentifier, StringComparison.OrdinalIgnoreCase))
							.ToList();
						if (exactMatches.Count == 0)
						{
							continue;
						}

						bestMatch = exactMatches.MaxBy(r => GetCurrencyMatchScore(r, expectedCurrency, symbolCurrencies));
						break;
					}
					if (bestMatch == null)
					{
						var identifierValues = symbolIdentifiers
							.Select(i => i.Identifier)
							.Where(v => !string.IsNullOrWhiteSpace(v))
							.Concat(symbolIdentifiers
								.Select(i => SymbolNameCleaner.CleanTickerSymbol(i.Identifier))
								.Where(v => !string.IsNullOrWhiteSpace(v)))
							.Distinct(StringComparer.OrdinalIgnoreCase)
							.ToArray();
						bestMatch = searchResults
							.OrderByDescending(r => SemanticMatcher.CalculateSemanticMatchScore(identifierValues, new[] { r.Symbol, SymbolNameCleaner.CleanTickerSymbol(r.Symbol), r.ShortName ?? string.Empty }))
							.ThenByDescending(r => GetCurrencyMatchScore(r, expectedCurrency, symbolCurrencies))
							.First();
					}
					bestMatch ??= searchResults[0];
					return await CreateSymbolProfileFromMatch(bestMatch);
				}))!;
			});
		}

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
		{
			string cacheKey = $"{symbol.Symbol}:{fromDate:yyyyMMdd}";
			IEnumerable<MarketData>? result = await cacheService.GetOrAddAsync(Source.Yahoo, TypeOfData.MarketData, cacheKey, async () =>
			{
				return await GetStockMarketData(symbol.Symbol, symbol.Currency, fromDate);
			});
			return result ?? [];
		}

		public async Task<IEnumerable<StockSplit>> GetStockSplits(SymbolProfile symbol, DateOnly fromDate)
		{
			var list = new List<StockSplit>();

			try
			{
				IReadOnlyList<SplitTick> history = await YahooFinanceApi.Yahoo.GetSplitsAsync(symbol.Symbol, new DateTime(fromDate, TimeOnly.MinValue, DateTimeKind.Utc), DateTime.Today);

				foreach (SplitTick? candle in history)
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

		private async Task<List<SearchResult>> GetSearchResultsForIdentifiers(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			var matches = new List<SearchResult>();

			foreach (PartialSymbolIdentifier identifier in symbolIdentifiers)
			{
				if (string.IsNullOrWhiteSpace(identifier.Identifier))
				{
					continue;
				}

				var searchTerm = PrepareSearchTerm(identifier);
				SearchResult[]? results = await SearchSymbol(searchTerm);

				if (results != null)
				{
					IEnumerable<SearchResult> filteredResults = results.Where(result => IsAllowedSymbolType(result, identifier));
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
			IReadOnlyDictionary<string, Security>? symbols = await GetSymbolDetails(match.Symbol);
			if (symbols == null)
			{
				return null;
			}

			Security? symbol = symbols.GetValueOrDefault(match.Symbol);
			if (symbol == null)
			{
				return null;
			}

			SecurityProfile? securityProfile = await GetSecurityProfile(symbol.Symbol);

			return new SymbolProfile(
				symbol.Symbol,
				GetName(symbol),
				[
					new SymbolIdentifier { Identifier = symbol.Symbol, IdentifierType = IdentifierType.Ticker },
					.. ListExtensions.FilterEmpty(new string?[] { GetName(symbol) }).Select(n => new SymbolIdentifier { Identifier = n, IdentifierType = IdentifierType.Name })
				],
				Currency.GetCurrency(symbol.Currency),
				Datasource.YAHOO,
				ParseQuoteType(symbol.QuoteType),
				ParseQuoteTypeAsSub(symbol.QuoteType),
				GetCountries(securityProfile),
				GetSectors(securityProfile))
			{
				WebsiteUrl = $"https://finance.yahoo.com/quote/{symbol.Symbol}",
			};
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
			if (identifier.AllowedAssetClasses == null || identifier.AllowedAssetClasses.Count == 0)
			{
				return true;
			}

			if (!identifier.AllowedAssetClasses.Contains(ParseQuoteType(searchResult.Type)))
			{
				return false;
			}

			if (identifier.AllowedAssetSubClasses == null || identifier.AllowedAssetSubClasses.Count == 0)
			{
				return true;
			}

			AssetSubClass? assetSubClass = ParseQuoteTypeAsSub(searchResult.Type);
			return assetSubClass == null || identifier.AllowedAssetSubClasses.Contains(assetSubClass.Value);
		}

		private async Task<IEnumerable<MarketData>> GetStockMarketData(string symbol, Currency currency, DateOnly fromDate)
		{
			IReadOnlyList<Candle> history = await RetryPolicyHelper.GetFallbackPolicy<IReadOnlyList<Candle>>(logger)
					.WrapAsync(RetryPolicyHelper.GetRetryPolicy(logger))
					.ExecuteAsync(() => YahooFinanceApi.Yahoo.GetHistoricalAsync(symbol, new DateTime(fromDate, TimeOnly.MinValue, DateTimeKind.Utc), null, Period.Daily));

			var list = new List<MarketData>();
			if (history != null)
			{
				foreach (Candle candle in history)
				{
					var item = new MarketData(
										currency,
										candle.Close,
										candle.Open,
										candle.High,
										candle.Low,
										candle.Volume,
										DateOnly.FromDateTime(candle.DateTime.Date));
					list.Add(item);
				}
			}

			// Always update today's price with the latest
			var today = DateOnly.FromDateTime(DateTime.Now);
			IReadOnlyDictionary<string, Security> symbolFields = await YahooFinanceApi.Yahoo.Symbols(symbol)
				.Fields(
					Field.RegularMarketPrice,
					Field.RegularMarketOpen,
					Field.RegularMarketDayHigh,
					Field.RegularMarketDayLow,
					Field.RegularMarketVolume)
				.QueryAsync();
			_ = symbolFields.TryGetValue(symbol, out Security? symbolItem);

			if (symbolItem != null)
			{
				var marketVolume = symbolItem.Fields.ContainsKey("RegularMarketVolume") ? symbolItem.RegularMarketVolume : 0;

				var item = new MarketData(
					currency,
					(decimal)symbolItem.RegularMarketPrice,
					(decimal)symbolItem.RegularMarketOpen,
					(decimal)symbolItem.RegularMarketDayHigh,
					(decimal)symbolItem.RegularMarketDayLow,
					marketVolume,
					today);

				// Remove any existing entry for today
				_ = list.RemoveAll(md => md.Date == today);
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

			return security.Fields.ContainsKey(Field.ShortName.ToString()) ? security.ShortName : security.Symbol;
		}

		private static SectorWeight[] GetSectors(SecurityProfile? securityProfile)
		{
			if (securityProfile is null)
			{
				return [];
			}

			return !securityProfile.Fields.ContainsKey(ProfileFields.Sector.ToString()) ? [] : [new SectorWeight(securityProfile.Sector, 1)];
		}

		private static CountryWeight[] GetCountries(SecurityProfile? securityProfile)
		{
			if (securityProfile is null)
			{
				return [];
			}

			return !securityProfile.Fields.ContainsKey(ProfileFields.Country.ToString())
				? []
				: [new CountryWeight(securityProfile.Country, string.Empty, string.Empty, 1)];
		}

		private static int GetCurrencyMatchScore(SearchResult result, Currency? expectedCurrency, IReadOnlyDictionary<string, Currency?> symbolCurrencies)
		{
			if (expectedCurrency == null)
			{
				return 0;
			}

			_ = symbolCurrencies.TryGetValue(result.Symbol, out Currency? actualCurrency);
			if (actualCurrency == null)
			{
				return 0;
			}

			// Normalize to source currency so that GBX/GBp and GBP are treated as equivalent
			(Currency? actualSource, _) = actualCurrency.GetSourceCurrency();
			(Currency? expectedSource, _) = expectedCurrency.GetSourceCurrency();

			return actualSource == expectedSource ? 1 : 0;
		}

		private async Task<Currency?> GetActualCurrencyAsync(string symbolName)
		{
			IReadOnlyDictionary<string, Security>? symbols = await GetSymbolDetails(symbolName);
			if (symbols == null)
			{
				return null;
			}

			_ = symbols.TryGetValue(symbolName, out Security? security);

			try
			{
				return security?.Currency is string currencySymbol ? Currency.GetCurrency(currencySymbol) : null;
			}
			catch (KeyNotFoundException ex)
			{
				logger.LogWarning(ex, "Failed to get currency for symbol {SymbolName}", symbolName);
				return null;
			}
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
