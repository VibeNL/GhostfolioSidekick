using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class GhostfolioMarketData(IRestCall restCall, ILogger<GhostfolioMarketData> logger) : IGhostfolioMarketData
	{
		public async Task DeleteSymbol(SymbolProfile symbolProfile)
		{
			try
			{
				var r = await restCall.DoRestDelete($"api/v1/admin/profile-data/{symbolProfile.DataSource}/{symbolProfile.Symbol}");
				if (!r.IsSuccessStatusCode)
				{
					throw new NotSupportedException($"Deletion failed {symbolProfile.Symbol}");
				}

				logger.LogDebug("Deleted symbol {Symbol}", symbolProfile.Symbol);
			}
			catch (NotAuthorizedException ex)
			{
				logger.LogWarning(ex, "403 Forbidden on DeleteSymbol for {Symbol} — non-admin user, skipping", symbolProfile.Symbol);
			}
		}

		public async Task<IEnumerable<SymbolProfile>> GetAllSymbolProfiles()
		{
			try
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
					content = await restCall.DoRestGet($"api/v1/market-data/{f.DataSource}/{f.Symbol}");
					var data = JsonConvert.DeserializeObject<MarketDataListNoMarketData>(content!);
					profiles.Add(data!.AssetProfile);
				}

				return profiles;
			}
			catch (NotAuthorizedException ex)
			{
				logger.LogWarning(ex, "403 Forbidden on GetAllSymbolProfiles — non-admin user, returning empty list");
				return [];
			}
		}

		public async Task<GenericInfo> GetBenchmarks()
		{
			var content = await restCall.DoRestGet($"api/v1/info/");
			return JsonConvert.DeserializeObject<GenericInfo>(content!)!;
		}
	}
}
