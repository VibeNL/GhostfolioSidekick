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

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
		{
			IEnumerable<MarketData>? result = await cacheService.GetOrAddAsync(CacheKey.CreateMarketData(Source.Yahoo, fromDate, DateOnly.MaxValue, symbol.Symbol), async () =>
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

				foreach (SplitTick candle in history)
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

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			if (!symbolIdentifiers.Any(x => !string.IsNullOrWhiteSpace(x.Identifier)))
			{
				return null;
			}

			string cacheKey = GenerateCacheKey(symbolIdentifiers);

			return await cacheService.GetOrAddAsync<SymbolProfile>(CacheKey.CreateSymbolProfile(Source.Yahoo, cacheKey), async () =>
			{
				AsyncRetryPolicy retryPolicy = RetryPolicyHelper.GetRetryPolicy(logger);
				return (await retryPolicy.ExecuteAsync(async () =>
				{
					var searchResults = await GetSearchResultsForIdentifiers(symbolIdentifiers);
					searchResults = [.. searchResults.OrderByDescending(x => x.SearchResult.Score)];
					if (searchResults.Length == 0)
					{
						return null;
					}

					// Find the best match
					// Prefer ISIN matches, then ticker matches (as we take the exchange into account), then full name matches, than partials.
					// Within those groups, prefer matches with the expected currency.

					// Match by ISIN
					var bestMatch = searchResults
						.FirstOrDefault(x => x.PartialSymbolIdentifier.IdentifierType == IdentifierType.ISIN);

					if (bestMatch != null)
					{
						return await CreateSymbolProfileFromMatch(bestMatch.SearchResult);
					}

					// Match by ticker
					bestMatch = searchResults
						.FirstOrDefault(x => x.PartialSymbolIdentifier.IdentifierType == IdentifierType.Ticker);

					if (bestMatch != null)
					{
						return await CreateSymbolProfileFromMatch(bestMatch.SearchResult);
					}

					// Match by name
					bestMatch = searchResults
						.OrderByDescending(x => SemanticMatcher.CalculateSemanticMatchScore(
							[x.PartialSymbolIdentifier.Identifier], 
							[x.SearchResult?.ShortName ?? ""]))
						.FirstOrDefault();

					if (bestMatch != null)
					{
						return await CreateSymbolProfileFromMatch(bestMatch.SearchResult);
					}

					return null;
				}))!;
			});
		}

		private async Task<CustomSearchResult[]> GetSearchResultsForIdentifiers(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			var matches = new List<CustomSearchResult>();

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

					IEnumerable<CustomSearchResult> filteredResultsWithCurrency = await Task.WhenAll(filteredResults.Select(async result =>
					{
						Currency? currency = await GetActualCurrencyAsync(result.Symbol);
						return new CustomSearchResult
						{
							PartialSymbolIdentifier = identifier,
							SearchResult = result,
							Currency = currency
						};
					}));
					matches.AddRange(filteredResultsWithCurrency);
				}
			}

			return [.. matches];
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

		internal static Currency ResolveExpectedCurrency(IEnumerable<PartialSymbolIdentifier> symbolIdentifiers)
		{
			return symbolIdentifiers
				.Where(i => i.Currency != null)
				.Select(i => i.Currency)
				.FirstOrDefault() ?? Currency.EUR;
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

		private static string GenerateCacheKey(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			// Include all identifiers, expected currency and asset classes in the cache key
			// to avoid returning stale results when the same ticker is looked up with different constraints.
			var identifierPart = string.Join("|", symbolIdentifiers
				.Where(x => !string.IsNullOrWhiteSpace(x.Identifier))
				.Select(x => x.Identifier));
			var currencyPart = symbolIdentifiers
				.Where(i => i.Currency != null)
				.Select(i => i.Currency!.Symbol)
				.FirstOrDefault() ?? string.Empty;
			var assetClassPart = string.Join(",", symbolIdentifiers
				.SelectMany(i => i.AllowedAssetClasses ?? [])
				.Distinct()
				.OrderBy(x => x));
			string cacheKey = $"{identifierPart}:{currencyPart}:{assetClassPart}";
			return cacheKey;
		}

		private class CustomSearchResult
		{
			public required PartialSymbolIdentifier PartialSymbolIdentifier { get; set; }

			public required SearchResult SearchResult { get; set; }
			public Currency? Currency { get; set; }
		}
	}
}
