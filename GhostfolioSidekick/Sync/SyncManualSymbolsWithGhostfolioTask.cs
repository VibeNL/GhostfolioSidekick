using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Parsers;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Sync
{
	internal class SyncManualSymbolsWithGhostfolioTask(
			IDbContextFactory<DatabaseContext> databaseContextFactory,
			IGhostfolioSync ghostfolioSync,
			ICurrencyExchange currencyExchange) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncManualActivitiesWithGhostfolio;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			await using var databaseContext = databaseContextFactory.CreateDbContext();
			var manualSymbolProfiles = await databaseContext.SymbolProfiles.Where(x => x.DataSource == Datasource.MANUAL).ToListAsync();
			await ghostfolioSync.SyncSymbolProfiles(manualSymbolProfiles);

			foreach (var profile in manualSymbolProfiles)
			{
				var list = CalculatePricesBasedOnActivityUnitPrice(databaseContext, profile);
			}
		}

		private async Task<ICollection<MarketData>> CalculatePricesBasedOnActivityUnitPrice(
				DatabaseContext databaseContext,
				SymbolProfile profile)
		{
			var activities = await databaseContext.Activities
				.OfType<BuySellActivity>()
				.Where(x => x.Holding != null && x.Holding.SymbolProfiles.Contains(profile))
				.OrderBy(x => x.Date)
				.ToListAsync();

			var minDate = activities.Min(x => x.Date).Date;
			var list = new List<MarketData>();
			for (var date = minDate; date < DateTime.Today; date = date.AddDays(1))
			{
				var fromActivity = activities.Last(x => x.Date <= date);
				var toActivity = activities.FirstOrDefault(x => x.Date > date);
				var expectedPrice = await CalculateExpectedPrice(profile.Currency, fromActivity, toActivity, date);
				list.Add(new MarketData
				{
					Date = DateOnly.FromDateTime(date),
					Close = new Model.Money(fromActivity.UnitPrice!.Currency, expectedPrice),
					Open = new Model.Money(fromActivity.UnitPrice.Currency, expectedPrice),
					High = new Model.Money(fromActivity.UnitPrice.Currency, expectedPrice),
					Low = new Model.Money(fromActivity.UnitPrice.Currency, expectedPrice),
				});
			}

			return list;
		}

		private async Task<decimal> CalculateExpectedPrice(Model.Currency currency, BuySellActivity fromActivity, BuySellActivity? toActivity, DateTime date)
		{
			var a = (decimal)(date - fromActivity.Date).TotalDays;
			var b = (decimal)((toActivity?.Date ?? DateTime.Today.AddDays(1)) - date).TotalDays;

			var percentage = a / (a + b);
			decimal amountFrom = (await currencyExchange.ConvertMoney(fromActivity.UnitPrice, currency, DateOnly.FromDateTime(date))).Amount;
			decimal amountTo = (await currencyExchange.ConvertMoney(toActivity?.UnitPrice ?? fromActivity.UnitPrice ?? new Model.Money(currency, 0), currency, DateOnly.FromDateTime(date))).Amount;
			return amountFrom + percentage * (amountTo - amountFrom);
		}
	}
}
