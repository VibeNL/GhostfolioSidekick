using GhostfolioSidekick.Activities.Strategies;
using GhostfolioSidekick.Database.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Activities
{
	internal class CalculatePriceTask(IEnumerable<IHoldingStrategy> holdingStrategies, IActivityRepository activityRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CalculatePrice;

		public TimeSpan ExecutionFrequency => TimeSpan.FromMinutes(5);

		public async Task DoWork()
		{
			var holdings = await activityRepository.GetAllHoldings();
			foreach (var holdingStrategy in holdingStrategies.OrderBy(x => x.Priority))
			{
				foreach (var holding in holdings)
				{
					await holdingStrategy.Execute(holding);
					await activityRepository.Store(holding);
				}
			}
		}
	}
}
