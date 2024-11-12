using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public class GhostfolioSync : IGhostfolioSync
	{
		private readonly IApiWrapper apiWrapper;

		public GhostfolioSync(IApplicationSettings settings, IApiWrapper apiWrapper)
		{
			ArgumentNullException.ThrowIfNull(settings);
			this.apiWrapper = apiWrapper ?? throw new ArgumentNullException(nameof(apiWrapper));
		}

		public async Task SyncAccount(Account account)
		{
			if (account.Platform != null)
			{
				var platform = await apiWrapper.GetPlatformByName(account.Platform.Name);
				if (platform == null)
				{
					await apiWrapper.CreatePlatform(account.Platform);
				}
			}

			var ghostFolioAccount = await apiWrapper.GetAccountByName(account.Name);
			if (ghostFolioAccount == null)
			{
				await apiWrapper.CreateAccount(account);
			}

			await apiWrapper.UpdateAccount(account);
		}

		public Task SyncAll(IEnumerable<Activity> allActivities)
		{
			throw new NotImplementedException();
		}
	}
}
