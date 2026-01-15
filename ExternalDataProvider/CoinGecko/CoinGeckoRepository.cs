using CoinGecko.Net.Interfaces;
using CoinGecko.Net.Objects.Models;
using CryptoExchange.Net.Objects;
using GhostfolioSidekick.Cryptocurrency;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.ExternalDataProvider.CoinGecko
{
	public class CoinGeckoRepository(
			ILogger<CoinGeckoRepository> logger,
			IMemoryCache memoryCache,
			ICoinGeckoRestClient coinGeckoRestClient
			) :
		ISymbolMatcher,
		IStockPriceRepository
	{
		public string DataSource => Datasource.COINGECKO;

		public DateOnly MinDate => DateOnly.FromDateTime(DateTime.Today.AddDays(-365));

		public bool AllowedForDeterminingHolding => true;

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			foreach (var id in symbolIdentifiers)
			{
				if (id.AllowedAssetSubClasses == null || !id.AllowedAssetSubClasses.Contains(AssetSubClass.CryptoCurrency))
				{
					continue;
				}

				var coinGeckoAsset = await GetCoinGeckoAsset(id.Identifier);
				if (coinGeckoAsset == null)
				{
					continue;
				}

				var symbolProfile = new SymbolProfile(
					coinGeckoAsset.Symbol,
					coinGeckoAsset.Name,
					[coinGeckoAsset.Symbol, coinGeckoAsset.Name],
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
			}

			return null;
		}

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
		{
			var coinGeckoAsset = await GetCoinGeckoAsset(symbol.Symbol);
			if (coinGeckoAsset == null)
			{
				return [];
			}

			var longRange = await RetryPolicyHelper
							.GetFallbackPolicy<WebCallResult<CoinGeckoOhlc[]>>(logger)
							.WrapAsync(RetryPolicyHelper
								.GetRetryPolicy(logger))
								.ExecuteAsync(async () =>
								{
									var response = await coinGeckoRestClient.Api.GetOhlcAsync(coinGeckoAsset.Id, "usd", 365);

									if (response == null || !response.Success)
									{
										throw new InvalidOperationException(response?.Error?.Message);
									}

									return response;
								});

			var shortRange = await RetryPolicyHelper
							.GetFallbackPolicy<WebCallResult<CoinGeckoOhlc[]>>(logger)
							.WrapAsync(RetryPolicyHelper
								.GetRetryPolicy(logger))
								.ExecuteAsync(async () =>
								{
									var response = await coinGeckoRestClient.Api.GetOhlcAsync(coinGeckoAsset.Id, "usd", 30);

									if (response == null || !response.Success)
									{
										throw new InvalidOperationException(response?.Error?.Message);
									}

									return response;
								});

			var list = new List<MarketData>();
			foreach (var candle in ((longRange?.Data) ?? []).Union((shortRange?.Data ?? [])))
			{
				var item = new MarketData(
									new Money(Currency.USD with { }, candle.Close),
									new Money(Currency.USD with { }, candle.Open),
									new Money(Currency.USD with { }, candle.High),
									new Money(Currency.USD with { }, candle.Low),
									0,
									DateOnly.FromDateTime(candle.Timestamp.Date));
				list.Add(item);
			}

			// Add the existing market data
			list = [.. list.Union(symbol.MarketData)];

			var x = list.OrderByDescending(x => x.Date).DistinctBy(x => x.Date);
			return x;
		}

		private async Task<CoinGeckoAsset?> GetCoinGeckoAsset(string identifier)
		{
			// Check if in the cache
			if (string.IsNullOrWhiteSpace(identifier))
			{
				return null;
			}

			if (memoryCache.TryGetValue<List<CoinGeckoAsset>>(DataSource, out var cachedCoinGecko))
			{
				return GetAsset(identifier, cachedCoinGecko!);
			}

			var coinGeckoAssets = await coinGeckoRestClient.Api.GetAssetsAsync();
			if (coinGeckoAssets == null || !coinGeckoAssets.Success)
			{
				return null;
			}

			var list = new List<CoinGeckoAsset>();
			foreach (var asset in coinGeckoAssets.Data)
			{
				var cachedCoinGeckoAsset = new CoinGeckoAsset
				{
					Id = asset.Id,
					Name = asset.Name,
					Symbol = asset.Symbol
				};
				list.Add(cachedCoinGeckoAsset);
			}

			memoryCache.Set(DataSource, list, TimeSpan.FromDays(1));

			if (memoryCache.TryGetValue(DataSource, out cachedCoinGecko))
			{
				return GetAsset(identifier, cachedCoinGecko!);
			}

			return null;

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
			var fullName = CryptoMapper.Instance.GetFullname(x.Symbol) ?? string.Empty;
			return x.Name.Equals(fullName, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
