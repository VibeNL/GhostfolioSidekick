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
			throw new NotImplementedException();
		}

		public void SyncAll(IEnumerable<Activity> allActivities)
		{
			throw new NotImplementedException();
		}
	}
}
