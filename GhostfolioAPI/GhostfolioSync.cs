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

		public void SyncAccount(Account account)
		{
			if (account.Platform != null)
			{
				var platform = apiWrapper.GetPlatformByName(account.Platform.Name);
				if (platform == null)
				{
					apiWrapper.CreatePlatform(account.Platform);
				}
			}

			var ghostFolioAccount = apiWrapper.GetAccountByName(account.Name);
			if (ghostFolioAccount == null)
			{
				apiWrapper.CreateAccount(account);
			}

			apiWrapper.UpdateAccount(account);
		}

		public void SyncAll(IEnumerable<Activity> allActivities)
		{
			throw new NotImplementedException();
		}
	}
}
