using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class AutomatedStockSplitTask : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.AutomatedStockSplit;

		public TimeSpan ExecutionFrequency => TimeSpan.FromDays(1);

		public async Task DoWork()
		{
			var dbContext = await DatabaseContext.GetDatabaseContext();


		}
	}
}
