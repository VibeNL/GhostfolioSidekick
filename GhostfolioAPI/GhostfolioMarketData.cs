using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class GhostfolioMarketData(RestCall restCall, ILogger<GhostfolioMarketData> logger) : IGhostfolioMarketData
	{
		public async Task DeleteSymbol(SymbolProfile symbolProfile)
		{
			var r = await restCall.DoRestDelete($"api/v1/admin/profile-data/{symbolProfile.DataSource}/{symbolProfile.Symbol}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {symbolProfile.Symbol}");
			}

			logger.LogDebug("Deleted symbol {Symbol}", symbolProfile.Symbol);
		}

		public async Task<IEnumerable<SymbolProfile>> GetAllSymbolProfiles()
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/");

			if (content == null)
			{
				return [];
			}

			var market = JsonConvert.DeserializeObject<MarketDataList>(content);

			var profiles = new List<SymbolProfile>();
			foreach (var f in market?.MarketData
				.Where(x => !string.IsNullOrWhiteSpace(x.Symbol) && !string.IsNullOrWhiteSpace(x.DataSource))
				.ToList() ?? [])
			{
				content = await restCall.DoRestGet($"api/v1/admin/market-data/{f.DataSource}/{f.Symbol}");
				var data = JsonConvert.DeserializeObject<MarketDataListNoMarketData>(content!);
				profiles.Add(data!.AssetProfile);
			}

			return profiles;
		}

		public async Task<GenericInfo> GetBenchmarks()
		{
			var content = await restCall.DoRestGet($"api/v1/info/");
			return JsonConvert.DeserializeObject<GenericInfo>(content!)!;
		}
	}
}
