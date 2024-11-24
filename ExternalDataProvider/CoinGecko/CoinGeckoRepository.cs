﻿using CoinGecko.Net.Clients;
using CoinGecko.Net.Objects.Models;
using CryptoExchange.Net.Objects;
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
			IMemoryCache memoryCache) :
		ISymbolMatcher,
		IStockPriceRepository
	{
		public string DataSource => Datasource.COINGECKO;

		public DateOnly MinDate => DateOnly.FromDateTime(DateTime.Today.AddDays(-365));

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] identifiers)
		{
			using var restClient = new CoinGeckoRestClient();
			foreach (var id in identifiers)
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

				var symbolProfile = new SymbolProfile(coinGeckoAsset.Symbol, coinGeckoAsset.Name, [], Currency.USD with { }, Datasource.COINGECKO, AssetClass.Liquidity, AssetSubClass.CryptoCurrency, [], []);
				return symbolProfile;
			}

			return null;
		}

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
		{
			using var restClient = new CoinGeckoRestClient();

			var coinGeckoAsset = await GetCoinGeckoAsset(symbol.Symbol);
			if (coinGeckoAsset == null)
			{
				return [];
			}

			var longRange = await RetryPolicyHelper
							.GetFallbackPolicy<WebCallResult<IEnumerable<CoinGeckoOhlc>>>(logger)
							.WrapAsync(RetryPolicyHelper
								.GetRetryPolicy(logger))
								.ExecuteAsync(async () =>
								{
									var response = await restClient.Api.GetOhlcAsync(coinGeckoAsset.Id, "usd", 365);

									if (response == null || !response.Success)
									{
										throw new InvalidOperationException(response?.Error?.Message);
									}

									return response;
								});

			var shortRange = await RetryPolicyHelper
							.GetFallbackPolicy<WebCallResult<IEnumerable<CoinGeckoOhlc>>>(logger)
							.WrapAsync(RetryPolicyHelper
								.GetRetryPolicy(logger))
								.ExecuteAsync(async () =>
								{
									var response = await restClient.Api.GetOhlcAsync(coinGeckoAsset.Id, "usd", 30);

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
			list = list.Union(symbol.MarketData).ToList();

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

			using var restClient = new CoinGeckoRestClient();
			var coinGeckoAssets = await restClient.Api.GetAssetsAsync();
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

			if (memoryCache.TryGetValue<List<CoinGeckoAsset>>(DataSource, out cachedCoinGecko))
			{
				return GetAsset(identifier, cachedCoinGecko!);
			}

			return null;

			static CoinGeckoAsset? GetAsset(string identifier, List<CoinGeckoAsset> cachedCoinGecko)
			{
				return cachedCoinGecko!
					.Where(x => x.Symbol.ToLowerInvariant() == identifier.ToLowerInvariant())
					.OrderBy(x => x.Name.Length) // Prefer the shortest name
					.ThenBy(x => x.Id)
					.FirstOrDefault();

			}
		}
	}
}
