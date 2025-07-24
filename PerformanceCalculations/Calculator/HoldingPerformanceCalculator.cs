using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PerformanceCalculations.Calculator
{
	public class HoldingPerformanceCalculator(DatabaseContext databaseContext, ICurrencyExchange currencyExchange) : IHoldingPerformanceCalculator
	{
		public async Task<IEnumerable<HoldingAggregated>> GetCalculatedHoldings(Currency targetCurrency)
		{
			// Preload all exchange rates for better performance
			await currencyExchange.PreloadAllExchangeRates();

			// Step 1: Get Holdings with SymbolProfiles (optimized projection)
			var holdingData = await databaseContext
				.Holdings
				.Where(x => x.SymbolProfiles.Any())
				.AsNoTracking()
				.Select(x => new
				{
					HoldingId = x.Id,
					SymbolProfiles = x.SymbolProfiles.Select(sp => new
					{
						sp.Symbol,
						sp.Name,
						sp.DataSource,
						sp.AssetClass,
						sp.AssetSubClass,
						sp.CountryWeight,
						sp.SectorWeights
					}).ToList()
				})
				.ToListAsync();

			if (!holdingData.Any())
			{
				return [];
			}

			var holdingIds = holdingData.Select(x => x.HoldingId).ToList();

			// Step 2: Get Activities separately to avoid Cartesian product
			var activitiesData = await databaseContext
				.Activities
				.Where(x => x.Holding != null && holdingIds.Contains(x.Holding.Id))
				.AsNoTracking()
				.Select(x => new
				{
					HoldingId = x.Holding!.Id,
					Activity = x
				})
				.ToListAsync();

			var activitiesByHolding = activitiesData
				.GroupBy(x => x.HoldingId)
				.ToDictionary(g => g.Key, g => g.Select(x => x.Activity).ToList());

			// Step 3: Pre-load all MarketData for all symbol profiles to avoid N+1 queries
			var symbolProfileKeys = holdingData
				.SelectMany(x => x.SymbolProfiles)
				.Select(sp => new { sp.Symbol, sp.DataSource })
				.Distinct()
				.ToList();

			var allMarketData = new Dictionary<(string Symbol, string DataSource), Dictionary<DateOnly, Money>>();

			if (symbolProfileKeys.Any())
			{
				// Extract the symbol/datasource pairs to local variables to avoid closure in LINQ
				var symbols = symbolProfileKeys.Select(x => x.Symbol).ToList();
				var dataSources = symbolProfileKeys.Select(x => x.DataSource).ToList();

				var marketDataQuery = await databaseContext
					.MarketDatas
					.Where(md => symbols.Contains(EF.Property<string>(md, "SymbolProfileSymbol")) &&
								dataSources.Contains(EF.Property<string>(md, "SymbolProfileDataSource")))
					.AsNoTracking()
					.Select(md => new
					{
						Symbol = EF.Property<string>(md, "SymbolProfileSymbol"),
						DataSource = EF.Property<string>(md, "SymbolProfileDataSource"),
						md.Date,
						md.Close
					})
					.ToListAsync();

				// Filter the results in memory to match exact pairs since EF query is broader
				var filteredMarketData = marketDataQuery
					.Where(md => symbolProfileKeys.Any(sp => sp.Symbol == md.Symbol && sp.DataSource == md.DataSource))
					.ToList();

				// Group market data by symbol profile
				foreach (var group in filteredMarketData.GroupBy(x => new { x.Symbol, x.DataSource }))
				{
					allMarketData[(group.Key.Symbol, group.Key.DataSource)] = group
						.ToDictionary(x => x.Date, x => x.Close);
				}
			}

			// Step 4: Build result
			var returnList = new List<HoldingAggregated>(holdingData.Count);
			foreach (var data in holdingData)
			{
				var defaultSymbolProfile = data
					.SymbolProfiles
					.OrderBy(x => x.
						DataSource
						.Contains(Datasource.GHOSTFOLIO) ? 2 : 1)
					.FirstOrDefault();
				if (defaultSymbolProfile == null)
				{
					continue;
				}

				var activities = activitiesByHolding.GetValueOrDefault(data.HoldingId, []);

				// Create proper SymbolProfile objects for the calculation method
				var symbolProfiles = data.SymbolProfiles.Select(sp => new SymbolProfile
				{
					Symbol = sp.Symbol,
					Name = sp.Name,
					DataSource = sp.DataSource,
					AssetClass = sp.AssetClass,
					AssetSubClass = sp.AssetSubClass,
					CountryWeight = sp.CountryWeight,
					SectorWeights = sp.SectorWeights
				}).ToList();

				returnList.Add(new HoldingAggregated
				{
					ActivityCount = activities.Count,
					Symbol = defaultSymbolProfile.Symbol,
					Name = defaultSymbolProfile.Name,
					DataSource = defaultSymbolProfile.DataSource,
					AssetClass = defaultSymbolProfile.AssetClass,
					AssetSubClass = defaultSymbolProfile.AssetSubClass,
					CountryWeight = defaultSymbolProfile.CountryWeight,
					SectorWeights = defaultSymbolProfile.SectorWeights,
					CalculatedSnapshots = await CalculateSnapShots(targetCurrency, symbolProfiles, activities, allMarketData).ConfigureAwait(false)
				});
			}

			return returnList;
		}

		private async Task<ICollection<CalculatedSnapshot>> CalculateSnapShots(
			Currency targetCurrency,
			IList<SymbolProfile> symbolProfiles,
			ICollection<Activity> activities,
			Dictionary<(string Symbol, string DataSource), Dictionary<DateOnly, Money>> preLoadedMarketData)
		{
			if (activities.Count == 0)
			{
				return [];
			}

			var minDate = DateOnly.FromDateTime(activities.Min(x => x.Date));
			var maxDate = DateOnly.FromDateTime(DateTime.Today);
			
			var dayCount = maxDate.DayNumber - minDate.DayNumber + 1;
			var snapshots = new List<CalculatedSnapshot>(dayCount);

			var activitiesByDate = activities
				.OfType<BuySellActivity>()
				.GroupBy(x => DateOnly.FromDateTime(x.Date))
				.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Date).ToList());

			var previousSnapshot = new CalculatedSnapshot(minDate.AddDays(-1), 0, Money.Zero(targetCurrency), Money.Zero(targetCurrency), Money.Zero(targetCurrency), Money.Zero(targetCurrency));

			// Use pre-loaded market data instead of querying database
			Dictionary<DateOnly, Money> marketData = new(dayCount);
			foreach (SymbolProfile symbolProfile in symbolProfiles)
			{
				if (preLoadedMarketData.TryGetValue((symbolProfile.Symbol, symbolProfile.DataSource), out var symbolMarketData))
				{
					foreach (var kvp in symbolMarketData)
					{
						marketData.TryAdd(kvp.Key, kvp.Value);
					}
				}
			}

			var lastKnownMarketPrice = marketData
				.Where(x => x.Key <= minDate)
				.OrderByDescending(x => x.Key)
				.Select(x => x.Value)
				.FirstOrDefault() ?? Money.Zero(targetCurrency);

			for (var date = minDate; date <= maxDate; date = date.AddDays(1))
			{
				var snapshot = new CalculatedSnapshot(previousSnapshot)
				{
					Date = date,
				};

				if (activitiesByDate.TryGetValue(date, out var dayActivities))
				{
					foreach (var activity in dayActivities)
					{
						var convertedAdjustedUnitPrice = await currencyExchange.ConvertMoney(
							activity.AdjustedUnitPrice,
							targetCurrency,
							date).ConfigureAwait(false);

						snapshot.AverageCostPrice = CalculateAverageCostPrice(snapshot, convertedAdjustedUnitPrice, activity.Quantity);
						snapshot.Quantity = snapshot.Quantity + activity.AdjustedQuantity;
						snapshot.TotalInvested = snapshot.TotalInvested.Add(convertedAdjustedUnitPrice.Times(activity.AdjustedQuantity));
					}
				}

				var marketPrice = marketData.TryGetValue(date, out var closePrice) ? closePrice : lastKnownMarketPrice;
				var marketPriceConverted = await currencyExchange.ConvertMoney(
							marketPrice,
							targetCurrency,
							date).ConfigureAwait(false);
				snapshot.CurrentUnitPrice = marketPriceConverted;
				snapshot.TotalValue = marketPriceConverted.Times(snapshot.Quantity);

				snapshots.Add(snapshot);
				previousSnapshot = snapshot;
			}

			// Round the values to avoid floating point issues
			foreach (var snapshot in snapshots)
			{
				snapshot.AverageCostPrice = new Money(targetCurrency, Math.Round(snapshot.AverageCostPrice.Amount, Constants.NumberOfDecimals));
				snapshot.CurrentUnitPrice = new Money(targetCurrency, Math.Round(snapshot.CurrentUnitPrice.Amount, Constants.NumberOfDecimals));
				snapshot.TotalInvested = new Money(targetCurrency, Math.Round(snapshot.TotalInvested.Amount, Constants.NumberOfDecimals));
				snapshot.TotalValue = new Money(targetCurrency, Math.Round(snapshot.TotalValue.Amount, Constants.NumberOfDecimals));
				snapshot.Quantity = Math.Round(snapshot.Quantity, Constants.NumberOfDecimals);
			}

			return snapshots;
		}

		private static Money CalculateAverageCostPrice(CalculatedSnapshot snapshot, Money unitPriceActivity, decimal quantityActivity)
		{
			if (snapshot.Quantity == 0)
			{
				return unitPriceActivity;
			}

			return snapshot.TotalInvested.Add(unitPriceActivity.Times(quantityActivity))
				.SafeDivide(snapshot.Quantity + quantityActivity);
		}
	}
}
