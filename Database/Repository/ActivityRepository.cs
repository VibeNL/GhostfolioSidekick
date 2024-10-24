using GhostfolioSidekick.Database.Migrations;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Matches;
using GhostfolioSidekick.Model.Symbols;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace GhostfolioSidekick.Database.Repository
{
	public class ActivityRepository(DatabaseContext databaseContext) : IActivityRepository
	{
		public async Task<IEnumerable<Activity>> GetAllActivities()
		{
			return await databaseContext.Activities.ToListAsync();
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

		public async Task<bool> HasMatch(ICollection<PartialSymbolIdentifier> partialSymbolIdentifiers, string dataSource)
		{
			var match = await databaseContext.ActivitySymbols.SingleOrDefaultAsync(x => partialSymbolIdentifiers.Contains(x.PartialSymbolIdentifier) && x.SymbolProfile!.DataSource == dataSource);

			return match != null;
		}

		public async Task SetMatch(Activity activity, SymbolProfile symbolProfile)
		{
			var match = await databaseContext.ActivitySymbols.SingleOrDefaultAsync(x => x.Activity == activity && x.SymbolProfile == symbolProfile);

			if (match == null)
			{
				foreach (var item in ((IActivityWithPartialIdentifier)activity).PartialSymbolIdentifiers)
				{
					var newMatch = new ActivitySymbol
					{
						Activity = activity,
						SymbolProfile = symbolProfile,
						PartialSymbolIdentifier = item
					};

					await databaseContext.ActivitySymbols.AddAsync(newMatch);
				}
				
				await databaseContext.SaveChangesAsync();
			}
		}
	}
}
