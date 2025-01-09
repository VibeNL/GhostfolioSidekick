using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.Logging;
using YahooFinanceApi;

namespace GhostfolioSidekick.ExternalDataProvider.Yahoo
{
	public class YahooRepository :
		ICurrencyRepository,
		ISymbolMatcher,
		IStockPriceRepository,
		IStockSplitRepository
	{
		public string DataSource => Datasource.YAHOO;

		public DateOnly MinDate => DateOnly.MinValue;

		private readonly ILogger<YahooRepository> logger;

		public YahooRepository(ILogger<YahooRepository> logger)
		{
			this.logger = logger;
		}

		public Task<IEnumerable<MarketData>> GetCurrencyHistory(Currency currencyFrom, Currency currencyTo, DateOnly fromDate)
		{
			return GetStockMarketData($"{currencyFrom.Symbol.ToUpperInvariant()}{currencyTo.Symbol.ToUpperInvariant()}=X", currencyFrom, fromDate);
		}

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] identifiers)
		{
			var matches = new List<SearchResult>();
			foreach (var id in identifiers)
			{
				var identifier = id.Identifier;

				if (id.AllowedAssetSubClasses?.Contains(AssetSubClass.CryptoCurrency) ?? false)
				{
					identifier = $"{identifier}-USD";
				}

				var searchResults = await RetryPolicyHelper.GetFallbackPolicy<SearchResult[]>(logger).WrapAsync(RetryPolicyHelper.GetRetryPolicy(logger)).ExecuteAsync(() => YahooFinanceApi.Yahoo.SearchAsync(identifier));
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
			// TODO: Fix if score is available
			var bestMatch = matches./*OrderByDescending(x => x.Score).*/First();

			var symbols = await RetryPolicyHelper.GetFallbackPolicy<IReadOnlyDictionary<string, Security>>(logger).WrapAsync(RetryPolicyHelper.GetRetryPolicy(logger)).ExecuteAsync(() => YahooFinanceApi.Yahoo.Symbols(bestMatch.Symbol).QueryAsync());
			if (symbols == null)
			{
				return null;
			}

			var symbol = symbols.OrderBy(x => x.Value.Symbol == bestMatch.Symbol).First().Value;

			if (symbol == null)
			{
				return null;
			}

			var securityProfile = await RetryPolicyHelper.GetFallbackPolicy<SecurityProfile>(logger).WrapAsync(RetryPolicyHelper.GetRetryPolicy(logger)).ExecuteAsync(() => YahooFinanceApi.Yahoo.QueryProfileAsync(symbol.Symbol));

			var symbolProfile = new SymbolProfile(symbol.Symbol, GetName(symbol), [symbol.Symbol, GetName(symbol)], Currency.GetCurrency(symbol.Currency), Datasource.YAHOO, ParseQuoteType(symbol.QuoteType), ParseQuoteTypeAsSub(symbol.QuoteType), GetCountries(securityProfile), GetSectors(securityProfile));

			return symbolProfile;

			bool FilterOnAllowedType(SearchResult x, PartialSymbolIdentifier id)
			{
				if (id.AllowedAssetClasses == null)
				{
					return true;
				}

				if (!id.AllowedAssetClasses.Contains(ParseQuoteType(x.Type)))
				{
					return false;
				}

				if (id.AllowedAssetSubClasses == null)
				{
					return true;
				}

				return ParseQuoteTypeAsSub(x.Type) == null || id.AllowedAssetSubClasses.Contains(ParseQuoteTypeAsSub(x.Type).Value);
			}
		}

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
		{
			var history = await RetryPolicyHelper.GetFallbackPolicy<IReadOnlyList<Candle>>(logger)
					.WrapAsync(RetryPolicyHelper.GetRetryPolicy(logger))
					.ExecuteAsync(() =>
						{
							try
							{
								return
									YahooFinanceApi
										.Yahoo
										.GetHistoricalAsync(symbol.Symbol, new DateTime(fromDate, TimeOnly.MinValue, DateTimeKind.Utc), null, Period.Daily);
							}
							catch (RuntimeBinderException ex) when (ex.Message.Contains("'System.Dynamic.ExpandoObject'"))
							{
								// No data?
							}

							return Task.FromResult<IReadOnlyList<Candle>>(null!);
						});
			
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
					var item = new StockSplit(DateOnly.FromDateTime(candle.DateTime), BeforeSplit: candle.AfterSplit, AfterSplit: candle.BeforeSplit); // API has them mixed up
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

		private string GetName(Security security)
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

		private SectorWeight[] GetSectors(SecurityProfile? securityProfile)
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

		private CountryWeight[] GetCountries(SecurityProfile? securityProfile)
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
				case "CURRENCY":
				case "CRYPTOCURRENCY":
					return AssetClass.Liquidity;
				case "INDEX":
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
				case "CRYPTOCURRENCY":
					return AssetSubClass.CryptoCurrency;
				case "CURRENCY":
					return AssetSubClass.Undefined;
				case "MUTUALFUND":
					return AssetSubClass.Undefined;
				default:
					return null;
			};
		}
	}
}
