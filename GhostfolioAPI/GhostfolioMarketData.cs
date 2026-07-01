using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class GhostfolioMarketData(IRestCall restCall, ILogger<GhostfolioMarketData> logger, IApplicationSettings settings) : IGhostfolioMarketData
	{
		public async Task DeleteSymbol(SymbolProfile symbolProfile)
		{
			if (!settings.AllowAdminCalls)
			{
				logger.LogDebug("DeleteSymbol skipped: not authorized (non-admin user)");
				return;
			}

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
			if (!settings.AllowAdminCalls)
			{
				logger.LogDebug("GetAllSymbolProfiles skipped: not authorized (non-admin user)");
				return [];
			}

			try
			{
				var content = await restCall.DoRestGet($"api/v1/asset-profiles");

				if (content == null)
				{
					return [];
				}

				var assetProfiles = JsonConvert.DeserializeObject<AssetProfileList>(content);

				return [.. (assetProfiles?.AssetProfiles ?? [])
					.Where(x => !string.IsNullOrWhiteSpace(x.Symbol) && !string.IsNullOrWhiteSpace(x.DataSource))];
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
