using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Sync
{
	internal class SyncWithGhostfolioTask(IDbContextFactory<DatabaseContext> databaseContextFactory, ICurrencyExchange exchangeRateService) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncWithGhostfolio;

		public TimeSpan ExecutionFrequency => TimeSpan.FromMinutes(5);

		public async Task DoWork()
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();

			foreach (var account in databaseContext.Accounts.Select(x => new { x.Name, x.Id, x.Balance }))
			{
				var activities = (await databaseContext.Activities
										.Where(x => x.Account.Id == account.Id)
										.AsNoTracking()
										.ToListAsync())
										.OrderBy(x => x.Date);

				var balanceCalculator = new BalanceCalculator(exchangeRateService);
				var balance = await balanceCalculator.Calculate(Currency.EUR, activities);
				var currentBalance = balance.OrderByDescending(x => x.Date).FirstOrDefault();
			}
		}
	}
}
