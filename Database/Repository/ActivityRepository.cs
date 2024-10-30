using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Database.Repository
{
	public class ActivityRepository(DatabaseContext databaseContext) : IActivityRepository
	{
		public async Task<Holding?> FindHolding(IList<PartialSymbolIdentifier> ids)
		{
			return (await databaseContext.Holdings.ToListAsync()).SingleOrDefault(x => ids.Any(y => x.IdentifierContainsInList(y)));
		}

		public async Task<IEnumerable<Activity>> GetAllActivities()
		{
			return await databaseContext.Activities.ToListAsync();
		}

		public async Task<IEnumerable<Holding>> GetAllHoldings()
		{
			return await databaseContext.Holdings.ToListAsync();
		}

		public async Task Store(Holding holding)
		{
			if (holding.Id == 0)
			{
				await databaseContext.Holdings.AddAsync(holding);
			}
			else
			{
				databaseContext.Holdings.Update(holding);
			}

			await databaseContext.SaveChangesAsync();
		}

		public async Task StoreAll(IEnumerable<Activity> activities)
		{
			// Deduplicate entities
			var existingActivities = await databaseContext.Activities.ToListAsync();
			var existingTransactionIds = existingActivities.Select(x => x.TransactionId).ToList();
			var newTransactionIds = activities.Select(x => x.TransactionId).ToList();

			// Delete activities that are not in the new list
			foreach (var deletedTransaction in existingTransactionIds.Except(newTransactionIds))
			{
				databaseContext.Activities.RemoveRange(existingActivities.Where(x => x.TransactionId == deletedTransaction));
			}

			// Add activities that are not in the existing list
			foreach (var addedTransaction in newTransactionIds.Except(existingTransactionIds))
			{
				await databaseContext.Activities.AddRangeAsync(activities.Where(x => x.TransactionId == addedTransaction));
			}

			// Update activities that are in both lists
			foreach (var updatedTransaction in existingTransactionIds.Intersect(newTransactionIds))
			{
				var existingActivity = existingActivities.Single(x => x.TransactionId == updatedTransaction);
				var newActivity = activities.Single(x => x.TransactionId == updatedTransaction);

				var compareLogic = new CompareLogic() { Config = new ComparisonConfig { MaxDifferences = int.MaxValue, IgnoreObjectTypes = true, MembersToIgnore = ["Id"] } };
				ComparisonResult result = compareLogic.Compare(existingActivity, newActivity);

				if (!result.AreEqual)
				{
					databaseContext.Activities.Remove(existingActivity);
					await databaseContext.Activities.AddAsync(newActivity);
				}
			}

			await databaseContext.SaveChangesAsync();
		}

		
	}
}
