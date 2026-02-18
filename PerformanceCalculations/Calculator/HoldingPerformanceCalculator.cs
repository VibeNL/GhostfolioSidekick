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
	public class PerformanceCalculator(DatabaseContext databaseContext, ICurrencyExchange currencyExchange) : IPerformanceCalculator
	{
		public async Task<IEnumerable<CalculatedSnapshot>> GetCalculatedSnapshots(Holding holding, Currency currency)
		{
			// Step 1: Get Holdings with SymbolProfiles (optimized projection)
			var holdingData = await databaseContext
				.Holdings
				.Where(x => x.Id == holding.Id && x.SymbolProfiles.Any())
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
						sp.SectorWeights,
						sp.Currency
					}).ToList()
				})
				.ToListAsync();

			if (holdingData.Count == 0)
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
					AccountId = x.Account.Id,
					Activity = x
				})
				.ToListAsync();

			var activitiesByHolding = activitiesData
				.GroupBy(x => x.HoldingId)
				.ToDictionary(g => g.Key, g => g.Select(x => new { x.AccountId, x.Activity }).ToList());

			// Step 3: Pre-load all MarketData for all symbol profiles to avoid N+1 queries
			var symbolProfileKeys = holdingData
				.SelectMany(x => x.SymbolProfiles)
				.Select(sp => new { sp.Symbol, sp.DataSource })
				.Distinct()
				.ToList();

			var allMarketData = new Dictionary<(string Symbol, string DataSource), Dictionary<DateOnly, decimal>>();

			if (symbolProfileKeys.Count != 0)
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

			// Step 4: Build result - return flat list of all snapshots
			var allSnapshots = new List<CalculatedSnapshot>();

			foreach (var data in holdingData)
			{
				var defaultSymbolProfile = data
					.SymbolProfiles
					.Where(x => x.Currency != Currency.NONE)
					.OrderBy(x => x.
						DataSource
						.Contains(Datasource.GHOSTFOLIO) ? 2 : 1)
					.ThenBy(x => x.Name?.Length ?? 0)
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
					SectorWeights = sp.SectorWeights,
					Currency = sp.Currency
				}).ToList();

				var accountIds = activities.Select(x => x.AccountId)
					.Distinct()
					.ToList();

				foreach (var accountId in accountIds)
				{
					ICollection<Activity> activitiesForAccount = [.. activities.Where(x => x.AccountId == accountId).Select(x => x.Activity)];
					var snapshots = await CalculateSnapShots(
						currency,
						accountId,
						symbolProfiles,
						activitiesForAccount,
						allMarketData).ConfigureAwait(false);
					allSnapshots.AddRange(snapshots);
				}
			}

			return allSnapshots;
		}

		private async Task<ICollection<CalculatedSnapshot>> CalculateSnapShots(
			Currency targetCurrency,
			int accountId,
			IList<SymbolProfile> symbolProfiles,
			ICollection<Activity> activities,
			Dictionary<(string Symbol, string DataSource), Dictionary<DateOnly, decimal>> preLoadedMarketData)
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
			.OfType<ActivityWithQuantityAndUnitPrice>()
			.GroupBy(x => DateOnly.FromDateTime(x.Date))
			.ToDictionary(g => g.Key, g => g.OrderBy(x => x.Date).ToList());

			var previousSnapshot = new CalculatedSnapshot(Guid.NewGuid(), accountId, minDate.AddDays(-1), 0, targetCurrency, 0, 0, 0, 0);

			// Use pre-loaded market data instead of querying database
			Dictionary<DateOnly, decimal> marketData = new(dayCount);
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
			.FirstOrDefault();

			for (var date = minDate; date <= maxDate; date = date.AddDays(1))
			{
				var snapshot = new CalculatedSnapshot(previousSnapshot)
				{
					Date = date,
				};

				await ApplyActivitiesForDateAsync(
					activitiesByDate,
					date,
					snapshot,
					targetCurrency,
					currencyExchange
				).ConfigureAwait(false);

				var marketPrice = marketData.TryGetValue(date, out var closePrice) ? closePrice : lastKnownMarketPrice;
				lastKnownMarketPrice = marketPrice;
				var marketPriceMoney = new Money(targetCurrency, marketPrice);
				var marketPriceConverted = await currencyExchange.ConvertMoney(
					marketPriceMoney,
						targetCurrency,
					date).ConfigureAwait(false);
				snapshot.CurrentUnitPrice = marketPriceConverted.Amount;
				snapshot.TotalValue = marketPriceConverted.Amount * snapshot.Quantity;

				snapshots.Add(snapshot);
				previousSnapshot = snapshot;
			}

			// Round the values to avoid floating point issues
			foreach (var snapshot in snapshots)
			{
				snapshot.AverageCostPrice = Math.Round(snapshot.AverageCostPrice, Constants.NumberOfDecimals);
				snapshot.CurrentUnitPrice = Math.Round(snapshot.CurrentUnitPrice, Constants.NumberOfDecimals);
				snapshot.TotalInvested = Math.Round(snapshot.TotalInvested, Constants.NumberOfDecimals);
				snapshot.TotalValue = Math.Round(snapshot.TotalValue, Constants.NumberOfDecimals);
				snapshot.Quantity = Math.Round(snapshot.Quantity, Constants.NumberOfDecimals);
			}

			return snapshots;
		}

		private static async Task ApplyActivitiesForDateAsync(
		Dictionary<DateOnly, List<ActivityWithQuantityAndUnitPrice>> activitiesByDate,
		DateOnly date,
		CalculatedSnapshot snapshot,
		Currency targetCurrency,
		ICurrencyExchange currencyExchange)
		{
			if (!activitiesByDate.TryGetValue(date, out var dayActivities))
			{
				return;
			}

			foreach (var activity in dayActivities)
			{
				var convertedTotal = await currencyExchange.ConvertMoney(
					activity.TotalTransactionAmount,
					targetCurrency,
					date)
				.ConfigureAwait(false);

				var sign = 0;
				sign = activity switch
				{
					BuyActivity or ReceiveActivity or GiftAssetActivity or StakingRewardActivity => 1,
					SellActivity or SendActivity => -1,
					_ => throw new InvalidOperationException($"Unsupported activity type: {activity.GetType().Name}"),
				};

				if (sign == 1)
				{
					// For buy/receive/gift/staking, add the invested amount and update average cost price
					snapshot.TotalInvested += convertedTotal.Amount;
					snapshot.Quantity += activity.AdjustedQuantity;
					snapshot.AverageCostPrice = CalculateAverageCostPrice(snapshot); // quantity already added above
				}
				else
				{
					// For sell/send, first calculate cost basis reduction using current average cost price
					var costBasisReduction = snapshot.AverageCostPrice * activity.AdjustedQuantity;
					snapshot.TotalInvested -= costBasisReduction;
					snapshot.Quantity -= activity.AdjustedQuantity;

					// Average cost price remains the same after a sell (unless quantity becomes zero)
					if (snapshot.Quantity <= 0)
					{
						snapshot.AverageCostPrice = 0;
					}
				}
			}
		}

		private static decimal CalculateAverageCostPrice(CalculatedSnapshot snapshot)
		{
			if (snapshot.Quantity == 0)
			{
				return 0;
			}

			return snapshot.TotalInvested / snapshot.Quantity;
		}

	}
}
