using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.AccountMaintainer
{
	internal class BalanceMaintainerTask(IDbContextFactory<DatabaseContext> databaseContextFactory, ICurrencyExchange exchangeRateService) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.BalanceMaintainer;

		public TimeSpan ExecutionFrequency => TimeSpan.FromMinutes(5);

		public async Task DoWork()
		{
			List<AccountKey> accountKeys;
			using (var databaseContext = await databaseContextFactory.CreateDbContextAsync())
			{
				accountKeys = await databaseContext.Accounts.Select(x => new AccountKey { Name = x.Name, Id = x.Id }).ToListAsync();
			}

			foreach (var accountKey in accountKeys)
			{
				using (var databaseContext = await databaseContextFactory.CreateDbContextAsync())
				{
					var activities = (await databaseContext.Activities
										.Where(x => x.Account.Id == accountKey.Id)
										.AsNoTracking()
										.ToListAsync())
										.OrderBy(x => x.Date);

					var balanceCalculator = new BalanceCalculator(exchangeRateService);
					var balances = await balanceCalculator.Calculate(Currency.EUR, activities);

					var account = await databaseContext.Accounts.FindAsync(accountKey.Id)!;
					var existingBalances = account!.Balance;

					var compareLogic = new CompareLogic() { Config = new ComparisonConfig { MaxDifferences = int.MaxValue, IgnoreObjectTypes = true, MembersToIgnore = ["Id"] } };
					ComparisonResult result = compareLogic.Compare(existingBalances.OrderBy(x => x.Date), balances.OrderBy(x => x.Date));

					if (!result.AreEqual)
					{
						account.Balance.Clear();
						account.Balance.AddRange(balances);
						await databaseContext.SaveChangesAsync();
					}
				}
			}
		}

		private class AccountKey
		{
			public required string Name { get; set; }

			public int Id { get; set; }
		}
	}
}
