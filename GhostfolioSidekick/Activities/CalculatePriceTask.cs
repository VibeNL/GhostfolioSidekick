using GhostfolioSidekick.Database;
using GhostfolioSidekick.ProcessingService.Activities.Strategies;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.ProcessingService.Activities
{
	internal class CalculatePriceTask(IEnumerable<IHoldingStrategy> holdingStrategies, IDbContextFactory<DatabaseContext> databaseContextFactory) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CalculatePrice;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			using var databaseContext = databaseContextFactory.CreateDbContext();

			var holdings = await databaseContext.Holdings.ToListAsync();
			foreach (var holdingStrategy in holdingStrategies.OrderBy(x => x.Priority))
			{
				foreach (var holding in holdings)
				{
					await holdingStrategy.Execute(holding);
				}
			}

			await databaseContext.SaveChangesAsync();
		}
	}
}
