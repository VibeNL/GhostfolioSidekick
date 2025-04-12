using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Activities
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

			// Calculate and store TWR and AverageBuyPrice for each holding
			foreach (var holding in holdings)
			{
				holding.TWR = CalculateTWR(holding.Activities);
				holding.AverageBuyPrice = CalculateAverageBuyPrice(holding.Activities);
			}

			await databaseContext.SaveChangesAsync();
		}

		private decimal CalculateTWR(IEnumerable<Activity> activities)
		{
			decimal twr = 1m;
			decimal previousBalance = 0m;

			foreach (var activity in activities.OrderBy(x => x.Date))
			{
				decimal currentBalance = previousBalance + activity.Amount;
				if (previousBalance != 0)
				{
					twr *= (1 + (currentBalance - previousBalance) / previousBalance);
				}
				previousBalance = currentBalance;
			}

			return twr - 1;
		}

		private decimal CalculateAverageBuyPrice(IEnumerable<Activity> activities)
		{
			var buySellActivities = activities.OfType<BuySellActivity>().Where(x => x.Quantity > 0);
			if (!buySellActivities.Any())
			{
				return 0;
			}

			decimal totalAmount = 0;
			decimal totalQuantity = 0;

			foreach (var activity in buySellActivities)
			{
				totalAmount += activity.TotalTransactionAmount.Amount;
				totalQuantity += activity.Quantity;
			}

			return totalAmount / totalQuantity;
		}
	}
}
