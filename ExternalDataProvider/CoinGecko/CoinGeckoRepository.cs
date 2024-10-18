using CoinGecko.Net.Clients;
using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Interfaces;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider.Yahoo;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YahooFinanceApi;

namespace GhostfolioSidekick.ExternalDataProvider.CoinGecko
{
	public class CoinGeckoRepository(ILogger<CoinGeckoRepository> logger, DatabaseContext databaseContext) :
		ISymbolMatcher,
		IStockPriceRepository
	{
		public string DataSource => Datasource.COINGECKO;

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
				await databaseContext.SymbolProfiles.AddAsync(symbolProfile);
				await databaseContext.SaveChangesAsync();
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

			var r = await restClient.Api.GetOhlcAsync(coinGeckoAsset.Id, "usd", 365);

			if (r == null || !r.Success)
			{
				if (r?.ResponseStatusCode == HttpStatusCode.TooManyRequests)
				{
					Task.Delay(4000).Wait();
				}

				return [];
			}

			var list = new List<MarketData>();
			foreach (var candle in r.Data)
			{
				var item = new MarketData(
									new Money(Currency.USD with { }, candle.Close),
									new Money(Currency.USD with { }, candle.Open),
									new Money(Currency.USD with { }, candle.High),
									new Money(Currency.USD with { }, candle.Low),
									0,
									candle.Timestamp.Date);
				list.Add(item);
			}

			return list;
		}

		private async Task<Database.Caches.CachedCoinGeckoAsset?> GetCoinGeckoAsset(string identifier)
		{
			if (!await databaseContext.CachedCoinGeckoAssets.AnyAsync()) // TODO, needs to be updated sometimes
			{
				using var restClient = new CoinGeckoRestClient();
				var coinGeckoAssets = await restClient.Api.GetAssetsAsync();
				if (coinGeckoAssets == null || !coinGeckoAssets.Success)
				{
					return null;
				}
				databaseContext.CachedCoinGeckoAssets.RemoveRange(databaseContext.CachedCoinGeckoAssets);
				foreach (var asset in coinGeckoAssets.Data)
				{
					var cachedCoinGeckoAsset = new Database.Caches.CachedCoinGeckoAsset
					{
						Id = asset.Id,
						Name = asset.Name,
						Symbol = asset.Symbol
					};
					await databaseContext.CachedCoinGeckoAssets.AddAsync(cachedCoinGeckoAsset);
				}
				await databaseContext.SaveChangesAsync();
			}

			return await databaseContext.CachedCoinGeckoAssets
				.Where(x => x.Symbol.ToLower() == identifier.ToLower())
				.OrderByDescending(x => x.Id.ToLower() == x.Name.ToLower()) // prefer when the name is equal to the id (first to be added to the set
				.OrderBy(x => x.Name.Length) // prefer shorter names
				.FirstOrDefaultAsync();
		}
	}
}
