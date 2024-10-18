using CoinGecko.Net.Clients;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider.Yahoo;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			SymbolProfile? symbolProfile = null;

			using var restClient = new CoinGeckoRestClient();
			foreach (var id in identifiers)
			{
				if (id.AllowedAssetSubClasses == null || !id.AllowedAssetSubClasses.Contains(AssetSubClass.CryptoCurrency))
				{
					continue;
				}

				var asset = await restClient.Api.GetAssetDetailsAsync(id.Identifier);


			}

			return symbolProfile;
		}
	}
}
