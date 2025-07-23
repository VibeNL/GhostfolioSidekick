using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.PerformanceCalculations.Models;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GhostfolioSidekick.PerformanceCalculations.Calculator
{
	public class HoldingPerformanceCalculator(DatabaseContext databaseContext, ICurrencyExchange currencyExchange) : IHoldingPerformanceCalculator
	{
		public async Task<IEnumerable<HoldingAggregated>> GetCalculatedHoldings(Currency targetCurrency)
		{
			var holdingData = await databaseContext
				.Holdings
				.Include(x => x.Activities)
				.Include(x => x.SymbolProfiles)
				.Where(x => x.SymbolProfiles.Any())
				.AsNoTracking()
				.Select(x => new
			{
				Holding = x,
				SymbolProfiles = x.SymbolProfiles,
				Activities = x.Activities
			}).ToListAsync();

			var returnList = new List<HoldingAggregated>(holdingData.Count);
			foreach (var data in holdingData)
			{
				var defaultSymbolProfile = data.SymbolProfiles.FirstOrDefault();

				if (defaultSymbolProfile == null)
				{
					continue;
				}

				returnList.Add(new HoldingAggregated
				{
					ActivityCount = data.Activities.Count,
					Symbol = defaultSymbolProfile.Symbol,
					Name = defaultSymbolProfile.Name,
					DataSource = defaultSymbolProfile.DataSource,
					AssetClass = defaultSymbolProfile.AssetClass,
					AssetSubClass = defaultSymbolProfile.AssetSubClass,
					CountryWeight = defaultSymbolProfile.CountryWeight,
					SectorWeights = defaultSymbolProfile.SectorWeights,
					CalculatedSnapshots = await CalculateSnapShots(targetCurrency, data.SymbolProfiles, data.Activities).ConfigureAwait(false)
				});
			}

			return returnList;
		}

		private async Task<ICollection<CalculatedSnapshot>> CalculateSnapShots(
			Currency targetCurrency,
			IList<SymbolProfile> symbolProfiles,
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

			Dictionary<DateOnly, Money> marketData = new(dayCount);
			foreach (SymbolProfile symbolProfile in symbolProfiles)
			{
				await databaseContext
						.SymbolProfiles
						.Where(x => x.Symbol == symbolProfile.Symbol && x.DataSource == symbolProfile.DataSource)
						.SelectMany(x => x.MarketData)
						.AsNoTracking()
						.ForEachAsync(x => marketData.TryAdd(x.Date, x.Close));
			}

			var lastKnownMarketPrice = marketData
				.Where(x => x.Key <= minDate)
				.OrderByDescending(x => x.Key)
				.Select(x => x.Value)
				.FirstOrDefault() ?? Money.Zero(targetCurrency);

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
								: snapshot.Quantity != 0 
									? snapshot.AverageCostPrice.Subtract((snapshot.AverageCostPrice.Subtract(correctedAdjustedUnitPrice)).Times((activity.AdjustedQuantity / snapshot.Quantity)))
									: correctedAdjustedUnitPrice, // When snapshot.Quantity is 0, use the current activity's price as the new average cost price
							Quantity = snapshot.Quantity + activity.AdjustedQuantity,
							TotalInvested = snapshot.TotalInvested.Add(correctedAdjustedUnitPrice.Times(activity.AdjustedQuantity)),
						};
					}
				}

				var marketPrice = marketData.TryGetValue(date, out var closePrice) ? closePrice : lastKnownMarketPrice;
								snapshot = snapshot with
				{
					TotalValue = marketPrice.Times(snapshot.Quantity)
				};

				snapshots.Add(snapshot);
				previousSnapshot = snapshot;
			}

			return snapshots;
		}
	}
}
