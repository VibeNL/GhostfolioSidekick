using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.AccountMaintainer
{
	internal class BalanceMaintainerTask(IDbContextFactory<DatabaseContext> databaseContextFactory, ICurrencyExchange exchangeRateService) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.BalanceMaintainer;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			List<AccountKey> accountKeys;
			using (var databaseContext = await databaseContextFactory.CreateDbContextAsync())
			{
				accountKeys = await databaseContext.Accounts
					.Select(x => new AccountKey { Name = x.Name, Id = x.Id })
					.ToListAsync();
			}

			foreach (var accountKey in accountKeys)
			{
				using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
				var activities = await databaseContext.Activities
									.Where(x => x.Account.Id == accountKey.Id)
									.AsNoTracking()
									.ToListAsync();

				// Ensure activities is never null and order by date
				var orderedActivities = (activities ?? []).OrderBy(x => x.Date);

				var balanceCalculator = new BalanceCalculator(exchangeRateService);
				var balances = await balanceCalculator.Calculate(Currency.EUR, orderedActivities);

				var account = await databaseContext.Accounts.SingleAsync(x => x.Id == accountKey.Id)!;
				var existingBalances = account!.Balance ?? [];

				var compareLogic = new CompareLogic() { Config = new ComparisonConfig { MaxDifferences = int.MaxValue, IgnoreObjectTypes = true, MembersToIgnore = ["Id"] } };
				ComparisonResult result = compareLogic.Compare(existingBalances.OrderBy(x => x.Date), balances.OrderBy(x => x.Date));

				if (!result.AreEqual)
				{
					if (account.Balance != null)
					{
						account.Balance.Clear();
						account.Balance.AddRange(balances);
					}
					await databaseContext.SaveChangesAsync();
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
