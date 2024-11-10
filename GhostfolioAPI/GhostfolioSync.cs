//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.GhostfolioAPI.API;
//using GhostfolioSidekick.Model.Accounts;
//using GhostfolioSidekick.Model.Activities;

//namespace GhostfolioSidekick.GhostfolioAPI
//{
//	public class GhostfolioSync : IGhostfolioSync
//	{
//		private readonly RestCall restCall;

//		public GhostfolioSync(IApplicationSettings settings, RestCall restCall)
//		{
//			ArgumentNullException.ThrowIfNull(settings);
//			this.restCall = restCall ?? throw new ArgumentNullException(nameof(restCall));
//		}

//		public void SyncAccount(Account account)
//		{
//			throw new NotImplementedException();
//		}

//		public void SyncAll(IEnumerable<Activity> allActivities)
//		{
//			throw new NotImplementedException();
//		}
//	}
//}
