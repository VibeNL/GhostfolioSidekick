using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using KellermanSoftware.CompareNetObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.AccountMaintainer
{
	internal class BalanceMaintainerTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		ICurrencyExchange exchangeRateService,
		IApplicationSettings applicationSettings) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.BalanceMaintainer;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Balance Maintainer";

		public async Task DoWork(ILogger logger)
		{
			// Parse currencies from application settings
			var currencies = applicationSettings.ConfigurationInstance.Settings.Currencies.Select(Currency.GetCurrency).ToList();

			if (currencies.Count == 0)
			{
				logger.LogWarning("No currencies configured in settings. Using EUR as default.");
				currencies.Add(Currency.EUR);
			}

			List<AccountKey> accountKeys;
			using (var databaseContext = await databaseContextFactory.CreateDbContextAsync())
			{
				accountKeys = await databaseContext.Accounts
					.Select(x => new AccountKey(x.Name, x.Id))
					.ToListAsync();
			}

			foreach (var accountKey in accountKeys.Select(x => x.Id))
			{
				using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
				var activities = await databaseContext.Activities
									.Where(x => x.Account.Id == accountKey)
									.AsNoTracking()
									.ToListAsync();

				// Ensure activities is never null and order by date
				var orderedActivities = (activities ?? []).OrderBy(x => x.Date);

				var balanceCalculator = new BalanceCalculator(exchangeRateService);

				List<Balance> allBalances = [];
				foreach (var currency in currencies)
				{
					allBalances.AddRange(await balanceCalculator.Calculate(currency, orderedActivities));
				}

				var account = await databaseContext.Accounts.SingleAsync(x => x.Id == accountKey)!;
				var existingBalances = account!.Balance ?? [];

				var compareLogic = new CompareLogic() { Config = new ComparisonConfig { MaxDifferences = int.MaxValue, IgnoreObjectTypes = true, MembersToIgnore = ["Id"] } };
				ComparisonResult result = compareLogic.Compare(existingBalances.OrderBy(x => x.Date).ThenBy(x => x.Money.Currency.Symbol), allBalances.OrderBy(x => x.Date).ThenBy(x => x.Money.Currency.Symbol));

				if (!result.AreEqual)
				{
					if (account.Balance != null)
					{
						account.Balance.Clear();
						account.Balance.AddRange(allBalances);
					}
					await databaseContext.SaveChangesAsync();
				}
			}
		}

		private sealed record AccountKey(string Name, int Id);
	}
}
