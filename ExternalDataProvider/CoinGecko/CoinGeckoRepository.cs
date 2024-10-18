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

		public Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
		{
			throw new NotImplementedException();
		}

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] identifiers)
		{
			using var restClient = new CoinGeckoRestClient();
			foreach (var id in identifiers)
			{
				if (id.AllowedAssetSubClasses == null || !id.AllowedAssetSubClasses.Contains(AssetSubClass.CryptoCurrency))
				{
					continue;
				}

				var coinGeckoAssets = await GetCoinGeckoAsset(id.Identifier);
				if (coinGeckoAssets == null)
				{
					continue;
				}
				var symbolProfile = new SymbolProfile(coinGeckoAssets.Symbol, coinGeckoAssets.Name, [], Currency.USD, Datasource.COINGECKO, AssetClass.Liquidity, AssetSubClass.CryptoCurrency, [], []);
				await databaseContext.SymbolProfiles.AddAsync(symbolProfile);
				await databaseContext.SaveChangesAsync();
				return symbolProfile;
			}

			return null;
		}
		public async Task<Database.Caches.CachedCoinGeckoAsset?> GetCoinGeckoAsset(string identifier)
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

			return await databaseContext.CachedCoinGeckoAssets.SingleOrDefaultAsync(x => x.Symbol.ToLower() == identifier.ToLower());
		}
	}
}
