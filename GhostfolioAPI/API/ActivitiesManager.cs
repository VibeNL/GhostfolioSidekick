using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class ActivitiesManager : IActivitiesManager
	{
		private readonly IApplicationSettings settings;
		private readonly MemoryCache memoryCache;
		private readonly ILogger<MarketDataManager> logger;
		private readonly RestCall restCall;

		public ActivitiesManager(
				IApplicationSettings settings,
				MemoryCache memoryCache,
				RestCall restCall,
				ILogger<MarketDataManager> logger)
		{
			ArgumentNullException.ThrowIfNull(settings);
			ArgumentNullException.ThrowIfNull(memoryCache);

			this.settings = settings;
			this.memoryCache = memoryCache;
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.restCall = restCall ?? throw new ArgumentNullException(nameof(restCall));
		}

		public void UpdateActivities(IEnumerable<Holding> holdings)
		{
			var lst = new Dictionary<Account, Dictionary<SymbolProfile, List<Activity>>>();
			foreach (Holding holding in holdings.Where(x => x.SymbolProfile != null))
				foreach (Activity activity in holding.Activities)
				{
					lst.TryAdd(activity.Account, new Dictionary<SymbolProfile, List<Activity>>());
					var act = lst[activity.Account];
					act.TryAdd(holding.SymbolProfile!, new List<Activity>());
					var sp = act[holding.SymbolProfile!];
					sp.Add(activity);
				}
			/*


			foreach (var account in activitiesByAccount)
			{
				var balance = GetBalance(account.Balance);

				await UpdateBalance(account, balance);

				var newActivities = account.Activities
					.Select(x => modelToContractMapper.ConvertToGhostfolioActivity(account, x))
					.Where(x => x != null && x.Type != ActivityType.IGNORE)
					.Select(Round!)
					.Where(x => x.Quantity != 0 || x.Fee != 0)
					.ToList();

				var content = await restCall.DoRestGet($"api/v1/order?accounts={existingAccount.Id}", CacheDuration.Short());

				if (content == null)
				{
					return;
				}

				var existingActivities = JsonConvert.DeserializeObject<ActivityList>(content)?.Activities ?? [];

				var mergeOrders = MergeOrders(newActivities, existingActivities).OrderBy(x => x.Operation).OrderBy(x => x.Order1?.Date ?? x.Order2?.Date ?? DateTime.MaxValue).ToList();
				foreach (var mergeOrder in mergeOrders)
				{
					try
					{
						switch (mergeOrder.Operation)
						{
							case Operation.New:
								await WriteOrder(mergeOrder.Order1!);
								break;
							case Operation.Duplicate:
								// Nothing to do!
								break;
							case Operation.Updated:
								await DeleteOrder(mergeOrder.Order2!);
								await WriteOrder(mergeOrder.Order1!);
								break;
							case Operation.Removed:
								await DeleteOrder(mergeOrder.Order2!);
								break;
							default:
								throw new NotSupportedException();
						}
					}
					catch (Exception ex)
					{
						logger.LogError($"Transaction failed to write {ex}, skipping");
					}
				}
			}*/
		}
	}
}
