using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Sync
{
	internal class SyncActivitiesWithGhostfolioTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncActivitiesWithGhostfolio;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Sync Activities with Ghostfolio";

		public TimeSpan? MaxRunTime => TimeSpan.FromHours(1);

		public async Task DoWork(ILogger logger, CancellationToken cancellationToken)
		{
			await using var databaseContext = await databaseContextFactory.CreateDbContextAsync(cancellationToken);

			// Only sync activities for accounts that have SyncActivities enabled
			var allActivities = await databaseContext.Activities
				.Include(x => x.Holding)
				.Include(x => x.Account)
				.ToListAsync();

			await ghostfolioSync.SyncAllActivities(allActivities, cancellationToken);
		}
	}
}
