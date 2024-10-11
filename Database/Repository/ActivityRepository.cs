using GhostfolioSidekick.Model.Activities;
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

			// Deduplicate partial identifiers
			var list = await databaseContext.PartialSymbolIdentifiers.ToListAsync();
			foreach (var activity in activities.OfType<IActivityWithPartialIdentifier>())
			{
				foreach (var partialSymbolIdentifier in activity.PartialSymbolIdentifiers.ToList())
				{
					var existingPartialSymbolIdentifier = list.FirstOrDefault(CompareIdentifier(partialSymbolIdentifier));
					if (existingPartialSymbolIdentifier != null)
					{
						activity.PartialSymbolIdentifiers.Remove(partialSymbolIdentifier);
						activity.PartialSymbolIdentifiers.Add(existingPartialSymbolIdentifier);
						continue;
					}

					list.Add(partialSymbolIdentifier);
				}
			}

			await databaseContext.SaveChangesAsync();
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
