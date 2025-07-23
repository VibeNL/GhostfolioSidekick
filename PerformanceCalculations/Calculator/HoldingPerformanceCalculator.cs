using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.PerformanceCalculations.Models;

namespace GhostfolioSidekick.PerformanceCalculations.Calculator
{
	public class HoldingPerformanceCalculator(DatabaseContext databaseContext, ICurrencyExchange currencyExchange) : IHoldingPerformanceCalculator
	{
		public async Task<IEnumerable<HoldingAggregated>> GetCalculatedHoldings(Currency targetCurrency)
		{
			var holdings = databaseContext.Holdings.Where(x => x.SymbolProfiles.Any()).Select(
				x => new HoldingAggregated
				{
					ActivityCount = x.Activities.Count,
					Symbol = x.SymbolProfiles.First().Symbol,
					Name = x.SymbolProfiles.First().Name,
					DataSource = x.SymbolProfiles.First().DataSource,
					AssetClass = x.SymbolProfiles.First().AssetClass,
					AssetSubClass = x.SymbolProfiles.First().AssetSubClass,
					CountryWeight = x.SymbolProfiles.First().CountryWeight,
					SectorWeights = x.SymbolProfiles.First().SectorWeights,
					CalculatedSnapshots = CalculateSnapShots(targetCurrency, x.Activities, x.SymbolProfiles.First().MarketData)
				});

			return holdings.ToList();
		}

		private ICollection<CalculatedSnapshot> CalculateSnapShots(
			Currency targetCurrency,
			ICollection<Activity> activities,
			ICollection<Model.Market.MarketData> marketData)
		{
			var minDate = DateOnly.FromDateTime(activities.Min(x => x.Date));
			var maxDate = DateOnly.FromDateTime(activities.Max(x => x.Date));
			var snapshots = new List<CalculatedSnapshot>(maxDate.DayNumber - minDate.DayNumber + 1);

			var previousSnapshot = new CalculatedSnapshot(minDate.AddDays(-1), 0, Money.Zero(targetCurrency), Money.Zero(targetCurrency), Money.Zero(targetCurrency), Money.Zero(targetCurrency));

			for (var date = minDate; date <= maxDate; date = date.AddDays(1))
			{
				var snapshot = previousSnapshot with
				{
					Date = date,
				};

				foreach (var activity in activities.OfType<ActivityWithQuantityAndUnitPrice>().Where(x => DateOnly.FromDateTime(x.Date) == date).OrderBy(x => x.Date))
				{
					var correctedAdjustedUnitPrice = currencyExchange.ConvertMoney(
						activity.AdjustedUnitPrice,
						targetCurrency,
						DateOnly.FromDateTime(activity.Date));

					snapshot = snapshot with
					{
						AverageCostPrice = activity switch
						{
							BuySellActivity buyOrSell => buyOrSell.Quantity > 0 ? snapshot.AverageCostPrice.Add(buyOrSell.AdjustedUnitPrice.Subtract(snapshot.AverageCostPrice).SafeDivide(snapshot.Quantity + buyOrSell.AdjustedQuantity))
																				: snapshot.AverageCostPrice.Subtract((snapshot.AverageCostPrice.Subtract(buyOrSell.AdjustedUnitPrice)).Times((buyOrSell.AdjustedQuantity / snapshot.Quantity))),
							_ => snapshot.AverageCostPrice
						},
						Quantity = activity switch
						{
							BuySellActivity buyOrSell => snapshot.Quantity + buyOrSell.AdjustedQuantity,
							_ => snapshot.Quantity
						},
						TotalInvested = activity switch
						{
							BuySellActivity buyOrSell => snapshot.TotalInvested.Add(buyOrSell.AdjustedUnitPrice.Times(buyOrSell.AdjustedQuantity)),
							_ => snapshot.TotalInvested
						}
					};
				}

				snapshot = snapshot with
				{
					TotalValue = marketData.FirstOrDefault(x => x.Date == date)?.Close.Times(snapshot.Quantity) ?? Money.Zero(targetCurrency)
				};

				snapshots.Add(snapshot);
			}

			return snapshots;
		}
	}
}
