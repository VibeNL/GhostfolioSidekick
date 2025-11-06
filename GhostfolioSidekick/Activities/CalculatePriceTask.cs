using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Activities
{
	internal class CalculatePriceTask(IEnumerable<IHoldingStrategy> holdingStrategies, IDbContextFactory<DatabaseContext> databaseContextFactory) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CalculatePrice;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Calculate Price";

		public async Task DoWork(ILogger logger)
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
