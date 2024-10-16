using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Matches;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;

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
			foreach (var activity in activities)
			{
				var existingActivity = existingActivities.FirstOrDefault(CompareActivity(activity));
				if (existingActivity != null)
				{
					continue;
				}

				await databaseContext.Activities.AddAsync(activity);
			}

			// Remove activities that are not in the new list
			foreach (var existingActivity in existingActivities)
			{
				if (!activities.Any(CompareActivity(existingActivity)))
				{
					databaseContext.Activities.Remove(existingActivity);
				}
			}

			await databaseContext.SaveChangesAsync();
		}

		public async Task SetMatch(Activity activty, SymbolProfile symbolProfile)
		{
			var match = await databaseContext.ActivitySymbols.SingleAsync(x => x.Activity == activty && x.SymbolProfile == symbolProfile);

			if (match == null)
			{
				var newMatch = new ActivitySymbol
				{
					Activity = activty,
					SymbolProfile = symbolProfile
				};

				await databaseContext.ActivitySymbols.AddAsync(newMatch);
				await databaseContext.SaveChangesAsync();
			}
		}

		private static Func<PartialSymbolIdentifier, bool> CompareIdentifier(PartialSymbolIdentifier partialSymbolIdentifier)
		{
			return p => p.Identifier == partialSymbolIdentifier.Identifier &&
						Enumerable.SequenceEqual(p.AllowedAssetClasses ?? [], partialSymbolIdentifier.AllowedAssetClasses ?? []) &&
						Enumerable.SequenceEqual(p.AllowedAssetSubClasses ?? [], partialSymbolIdentifier.AllowedAssetSubClasses ?? []);
		}

		private static Func<Activity, bool> CompareActivity(Activity activity)
		{
			return a => a.Date == activity.Date && a.TransactionId == activity.TransactionId;
		}
	}
}
