using CoinGecko.Net.Interfaces;
using CoinGecko.Net.Objects.Models;
using CryptoExchange.Net.Objects;
using GhostfolioSidekick.Cryptocurrency;
using GhostfolioSidekick.ExternalDataProvider.Cache;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly.Retry;

namespace GhostfolioSidekick.ExternalDataProvider.CoinGecko
{
	public class CoinGeckoRepository(
			ILogger<CoinGeckoRepository> logger,
			IMemoryCache memoryCache,
			ICoinGeckoRestClient coinGeckoRestClient,
			IExternalDataCacheService cacheService
			) :
		 ISymbolMatcher,
		 IStockPriceRepository
	{
		public string DataSource => Datasource.COINGECKO;

		public DateOnly MinDate => DateOnly.FromDateTime(DateTime.Today.AddDays(-365));

		public bool AllowedForDeterminingHolding => true;

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			// Use the first valid identifier for cache key
			PartialSymbolIdentifier? id = symbolIdentifiers.FirstOrDefault(x => x.AllowedAssetSubClasses != null && x.AllowedAssetSubClasses.Contains(AssetSubClass.CryptoCurrency));
			if (id == null)
			{
				return null;
			}

			return await cacheService.GetOrAddAsync<SymbolProfile>(CacheKey.CreateSymbolProfile(Source.CoinGecko, id.Identifier), async () =>
			{
				AsyncRetryPolicy retryPolicy = RetryPolicyHelper.GetRetryPolicy(logger);
				return (await retryPolicy.ExecuteAsync(async () =>
				{
					CoinGeckoAsset? coinGeckoAsset = await GetCoinGeckoAsset(id.Identifier);
					if (coinGeckoAsset == null)
					{
						return null;
					}

					SymbolProfile symbolProfile = new(
						coinGeckoAsset.Symbol,
						coinGeckoAsset.Name,
						[
							new SymbolIdentifier { Identifier = coinGeckoAsset.Symbol, IdentifierType = IdentifierType.Ticker },
						   new SymbolIdentifier { Identifier = coinGeckoAsset.Name, IdentifierType = IdentifierType.Name }
						],
						Currency.USD with { },
						Datasource.COINGECKO,
						AssetClass.Liquidity,
						AssetSubClass.CryptoCurrency,
						[],
						[])
					{
						WebsiteUrl = $"https://www.coingecko.com/en/coins/{coinGeckoAsset.Id}"
					};
					return symbolProfile;
				}))!;
			});
		}

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
		{
			IEnumerable<MarketData>? result = await cacheService.GetOrAddAsync(CacheKey.CreateMarketData(Source.CoinGecko, fromDate, DateOnly.MaxValue, symbol.Symbol), async () =>
			{
				CoinGeckoAsset? coinGeckoAsset = await GetCoinGeckoAsset(symbol.Symbol);
				if (coinGeckoAsset == null)
				{
					return [];
				}

				WebCallResult<CoinGeckoOhlc[]> longRange = await RetryPolicyHelper
								.GetFallbackPolicy<WebCallResult<CoinGeckoOhlc[]>>(logger)
								.WrapAsync(RetryPolicyHelper
									.GetRetryPolicy(logger))
									.ExecuteAsync(async () =>
									{
										WebCallResult<CoinGeckoOhlc[]>? response = await coinGeckoRestClient.Api.GetOhlcAsync(coinGeckoAsset.Id, "usd", 365);

										return response == null || !response.Success ? throw new InvalidOperationException(response?.Error?.Message) : response;
									});

				WebCallResult<CoinGeckoOhlc[]> shortRange = await RetryPolicyHelper
								.GetFallbackPolicy<WebCallResult<CoinGeckoOhlc[]>>(logger)
								.WrapAsync(RetryPolicyHelper
									.GetRetryPolicy(logger))
									.ExecuteAsync(async () =>
									{
										WebCallResult<CoinGeckoOhlc[]>? response = await coinGeckoRestClient.Api.GetOhlcAsync(coinGeckoAsset.Id, "usd", 30);

										return response == null || !response.Success ? throw new InvalidOperationException(response?.Error?.Message) : response;
									});

				List<MarketData> list = [];
				foreach (CoinGeckoOhlc candle in ((longRange?.Data) ?? []).Union(shortRange?.Data ?? []))
				{
					MarketData item = new(
									Currency.USD,
									candle.Close,
									candle.Open,
									candle.High,
									candle.Low,
									0,
									DateOnly.FromDateTime(candle.Timestamp.Date));
					list.Add(item);
				}

				// Add the existing market data
				list = [.. list.Union(symbol.MarketData)];

				IEnumerable<MarketData> x = list.OrderByDescending(x => x.Date).DistinctBy(x => x.Date);
				return x;
			});
			return result ?? [];
		}

		private async Task<CoinGeckoAsset?> GetCoinGeckoAsset(string identifier)
		{
			// Check if in the cache
			if (string.IsNullOrWhiteSpace(identifier))
			{
				return null;
			}

			if (memoryCache.TryGetValue<List<CoinGeckoAsset>>(DataSource, out List<CoinGeckoAsset>? cachedCoinGecko))
			{
				return GetAsset(identifier, cachedCoinGecko!);
			}

			WebCallResult<CoinGeckoAsset[]> coinGeckoAssets = await coinGeckoRestClient.Api.GetAssetsAsync();
			if (coinGeckoAssets == null || !coinGeckoAssets.Success)
			{
				return null;
			}

			List<CoinGeckoAsset> list = [];
			foreach (CoinGeckoAsset asset in coinGeckoAssets.Data)
			{
				CoinGeckoAsset cachedCoinGeckoAsset = new()
				{
					Id = asset.Id,
					Name = asset.Name,
					Symbol = asset.Symbol
				};
				list.Add(cachedCoinGeckoAsset);
			}

			_ = memoryCache.Set(DataSource, list, TimeSpan.FromDays(1));

			return memoryCache.TryGetValue(DataSource, out cachedCoinGecko) ? GetAsset(identifier, cachedCoinGecko!) : null;

			static CoinGeckoAsset? GetAsset(string identifier, List<CoinGeckoAsset> cachedCoinGecko)
			{
				return cachedCoinGecko!
					.Where(x => x.Symbol.Equals(identifier, StringComparison.InvariantCultureIgnoreCase))
					.OrderByDescending(IsKnown) // true should be first
					.ThenBy(x => x.Name.Length) // Prefer the shortest name
					.ThenBy(x => x.Id)
					.FirstOrDefault();
			}
		}

		private static bool IsKnown(CoinGeckoAsset x)
		{
			string fullName = CryptoMapper.Instance.GetFullname(x.Symbol) ?? string.Empty;
			return x.Name.Equals(fullName, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
