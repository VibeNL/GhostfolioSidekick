using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PerformanceCalculations.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PerformanceCalculations.Calculator
{
	public class HoldingPerformanceCalculator(DatabaseContext databaseContext, ICurrencyExchange currencyExchange) : IHoldingPerformanceCalculator
	{
		public async Task<IEnumerable<HoldingAggregated>> GetCalculatedHoldings(Currency targetCurrency)
		{
			var holdings = await databaseContext
				.Holdings
				.Include(x => x.Activities)
				.Include(x => x.SymbolProfiles)
				.Where(x => x.SymbolProfiles.Any())
				.AsNoTracking()
				.ToListAsync();

			var holdingData = holdings.Select(x => new
			{
				Holding = x,
				SymbolProfile = x.SymbolProfiles.First(),
				Activities = x.Activities
			}).ToList();

			var tasks = holdingData.Select(async data => new HoldingAggregated
			{
				ActivityCount = data.Activities.Count,
				Symbol = data.SymbolProfile.Symbol,
				Name = data.SymbolProfile.Name,
				DataSource = data.SymbolProfile.DataSource,
				AssetClass = data.SymbolProfile.AssetClass,
				AssetSubClass = data.SymbolProfile.AssetSubClass,
				CountryWeight = data.SymbolProfile.CountryWeight,
				SectorWeights = data.SymbolProfile.SectorWeights,
				CalculatedSnapshots = await CalculateSnapShots(targetCurrency, data.SymbolProfile, data.Activities).ConfigureAwait(false)
			});

			var converted = await Task.WhenAll(tasks);
			return converted;
		}

		private async Task<ICollection<CalculatedSnapshot>> CalculateSnapShots(
			Currency targetCurrency,
			SymbolProfile symbolProfile,
			ICollection<Activity> activities)
		{
			if (activities.Count == 0)
			{
				return [];
			}

			var minDate = DateOnly.FromDateTime(activities.Min(x => x.Date));
			var maxDate = DateOnly.FromDateTime(activities.Max(x => x.Date));
			
			var dayCount = maxDate.DayNumber - minDate.DayNumber + 1;
			var snapshots = new List<CalculatedSnapshot>(dayCount);

			var activitiesByDate = activities
				.OfType<BuySellActivity>()
				.GroupBy(x => DateOnly.FromDateTime(x.Date))
				.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Date).ToList());

			var previousSnapshot = new CalculatedSnapshot(minDate.AddDays(-1), 0, Money.Zero(targetCurrency), Money.Zero(targetCurrency), Money.Zero(targetCurrency), Money.Zero(targetCurrency));

			for (var date = minDate; date <= maxDate; date = date.AddDays(1))
			{
				var snapshot = previousSnapshot with
				{
					Date = date,
				};

				if (activitiesByDate.TryGetValue(date, out var dayActivities))
				{
					foreach (var activity in dayActivities)
					{
						var correctedAdjustedUnitPrice = await currencyExchange.ConvertMoney(
							activity.AdjustedUnitPrice,
							targetCurrency,
							DateOnly.FromDateTime(activity.Date)).ConfigureAwait(false);

						snapshot = snapshot with
						{
							AverageCostPrice = activity.Quantity > 0 
								? snapshot.AverageCostPrice.Add(correctedAdjustedUnitPrice.Subtract(snapshot.AverageCostPrice).SafeDivide(snapshot.Quantity + activity.AdjustedQuantity))
								: snapshot.AverageCostPrice.Subtract((snapshot.AverageCostPrice.Subtract(correctedAdjustedUnitPrice)).Times((activity.AdjustedQuantity / snapshot.Quantity))),
							Quantity = snapshot.Quantity + activity.AdjustedQuantity,
							TotalInvested = snapshot.TotalInvested.Add(correctedAdjustedUnitPrice.Times(activity.AdjustedQuantity)),
						};
					}
				}

				var marketData = await databaseContext
					.SymbolProfiles
					.Where(x => x.Symbol == symbolProfile.Symbol && x.DataSource == symbolProfile.DataSource)
					.SelectMany(x => x.MarketData)
					.Where(x => x.Date == date)
					.AsNoTracking()
					.FirstOrDefaultAsync();

				snapshot = snapshot with
				{
					TotalValue = marketData?.Close?.Times(snapshot.Quantity) ?? Money.Zero(targetCurrency)
				};

				snapshots.Add(snapshot);
				previousSnapshot = snapshot;
			}

			return snapshots;
		}
	}
}
