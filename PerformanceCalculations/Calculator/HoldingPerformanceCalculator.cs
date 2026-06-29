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
	public class PerformanceCalculator(IDbContextFactory<DatabaseContext> dbFactory, ICurrencyExchange currencyExchange) : IPerformanceCalculator
	{
		private bool hasPreloaded = false;

		public async Task<IEnumerable<CalculatedSnapshot>> GetCalculatedSnapshots(Holding holding, Currency currency)
		{
			if (!hasPreloaded)
			{
				// Ensure exchange rates are preloaded into cache
				await currencyExchange.PreloadAllExchangeRates();
				hasPreloaded = true;
			}

			using DatabaseContext databaseContext = await dbFactory.CreateDbContextAsync(CancellationToken.None);

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
				.FirstOrDefaultAsync();

			if (holdingData == null)
			{
				return [];
			}

			if (!holdingData.SymbolProfiles.Any(x => x.Currency != Currency.NONE))
			{
				return [];
			}

			var activitiesData = await databaseContext
				.Activities
				.Where(x => x.Holding != null && x.Holding.Id == holdingData.HoldingId)
				.AsNoTracking()
				.Select(x => new
				{
					AccountId = x.Account.Id,
					Activity = x
				})
				.ToListAsync(CancellationToken.None);

			Dictionary<int, List<Activity>> activitiesByAccount = [];
			foreach (var activityData in activitiesData)
			{
				if (!activitiesByAccount.TryGetValue(activityData.AccountId, out List<Activity>? activitiesForAccount))
				{
					activitiesForAccount = [];
					activitiesByAccount[activityData.AccountId] = activitiesForAccount;
				}

				activitiesForAccount.Add(activityData.Activity);
			}

			HashSet<(string Symbol, string DataSource)> symbolProfileKeys = [];
			List<string> symbols = [];
			List<string> dataSources = [];

			foreach (var symbolProfile in holdingData.SymbolProfiles)
			{
				if (symbolProfileKeys.Add((symbolProfile.Symbol, symbolProfile.DataSource)))
				{
					symbols.Add(symbolProfile.Symbol);
					dataSources.Add(symbolProfile.DataSource);
				}
			}

			Dictionary<(string Symbol, string DataSource), Dictionary<DateOnly, decimal>> allMarketData = [];
			if (symbolProfileKeys.Count != 0)
			{
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
						md.Close,
						md.Currency
					})
					.ToListAsync(CancellationToken.None);

				foreach (var marketDataRow in marketDataQuery)
				{
					if (marketDataRow.Close == 0 || !symbolProfileKeys.Contains((marketDataRow.Symbol, marketDataRow.DataSource)))
					{
						continue;
					}

					if (!allMarketData.TryGetValue((marketDataRow.Symbol, marketDataRow.DataSource), out Dictionary<DateOnly, decimal>? marketDataByDate))
					{
						marketDataByDate = [];
						allMarketData[(marketDataRow.Symbol, marketDataRow.DataSource)] = marketDataByDate;
					}

					if (marketDataRow.Currency == currency)
					{
						marketDataByDate[marketDataRow.Date] = marketDataRow.Close;
					}
					else
					{
						Money converted = await currencyExchange.ConvertMoney(new Money(marketDataRow.Currency, marketDataRow.Close), currency, marketDataRow.Date).ConfigureAwait(false);
						marketDataByDate[marketDataRow.Date] = converted.Amount;
					}
				}
			}

			List<SymbolProfile> symbolProfiles = new(holdingData.SymbolProfiles.Count);
			foreach (var symbolProfile in holdingData.SymbolProfiles)
			{
				symbolProfiles.Add(new SymbolProfile
				{
					Symbol = symbolProfile.Symbol,
					Name = symbolProfile.Name,
					DataSource = symbolProfile.DataSource,
					AssetClass = symbolProfile.AssetClass,
					AssetSubClass = symbolProfile.AssetSubClass,
					CountryWeight = symbolProfile.CountryWeight,
					SectorWeights = symbolProfile.SectorWeights,
					Currency = symbolProfile.Currency
				});
			}

			List<CalculatedSnapshot> allSnapshots = [];
			foreach (KeyValuePair<int, List<Activity>> accountActivities in activitiesByAccount)
			{
				ICollection<CalculatedSnapshot> snapshots = await CalculateSnapShots(
					currency,
					accountActivities.Key,
					symbolProfiles,
					accountActivities.Value,
					allMarketData).ConfigureAwait(false);
				allSnapshots.AddRange(snapshots);
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
			List<ActivityWithQuantityAndUnitPrice> quantityActivities = [];
			DateOnly minDate = default;
			bool hasQuantityActivities = false;

			foreach (Activity activity in activities)
			{
				if (activity is not ActivityWithQuantityAndUnitPrice quantityActivity)
				{
					continue;
				}

				quantityActivities.Add(quantityActivity);
				DateOnly activityDate = DateOnly.FromDateTime(quantityActivity.Date);
				if (!hasQuantityActivities || activityDate < minDate)
				{
					minDate = activityDate;
					hasQuantityActivities = true;
				}
			}

			if (!hasQuantityActivities)
			{
				return [];
			}

			DateOnly maxDate = DateOnly.FromDateTime(DateTime.Today);
			int dayCount = maxDate.DayNumber - minDate.DayNumber + 1;
			List<CalculatedSnapshot> snapshots = new(dayCount);

			Dictionary<DateOnly, List<ActivityWithQuantityAndUnitPrice>> activitiesByDate = [];
			foreach (ActivityWithQuantityAndUnitPrice activity in quantityActivities)
			{
				DateOnly activityDate = DateOnly.FromDateTime(activity.Date);
				if (!activitiesByDate.TryGetValue(activityDate, out List<ActivityWithQuantityAndUnitPrice>? activitiesForDate))
				{
					activitiesForDate = [];
					activitiesByDate[activityDate] = activitiesForDate;
				}

				activitiesForDate.Add(activity);
			}

			foreach (List<ActivityWithQuantityAndUnitPrice> activitiesForDate in activitiesByDate.Values)
			{
				if (activitiesForDate.Count > 1)
				{
					activitiesForDate.Sort((left, right) => left.Date.CompareTo(right.Date));
				}
			}

			CalculatedSnapshot previousSnapshot = new(Guid.NewGuid(), accountId, minDate.AddDays(-1), 0, targetCurrency, 0, 0, 0, 0);

			Dictionary<DateOnly, decimal> marketData = new(dayCount);
			foreach (SymbolProfile symbolProfile in symbolProfiles)
			{
				if (preLoadedMarketData.TryGetValue((symbolProfile.Symbol, symbolProfile.DataSource), out Dictionary<DateOnly, decimal>? symbolMarketData))
				{
					foreach (KeyValuePair<DateOnly, decimal> kvp in symbolMarketData)
					{
						_ = marketData.TryAdd(kvp.Key, kvp.Value);
					}
				}
			}

			decimal lastKnownMarketPrice = GetLastKnownMarketPrice(marketData, minDate);
			await AddActivitiesAsMarketDataIfNeeded(marketData, activitiesByDate, minDate, maxDate, targetCurrency).ConfigureAwait(false);

			for (DateOnly date = minDate; date <= maxDate; date = date.AddDays(1))
			{
				if (lastKnownMarketPrice == 0 && marketData.TryGetValue(date, out decimal knownPrice) && knownPrice != 0)
				{
					lastKnownMarketPrice = knownPrice;
				}

				CalculatedSnapshot snapshot = new(previousSnapshot)
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

				decimal marketPrice = marketData.TryGetValue(date, out decimal closePrice) ? closePrice : lastKnownMarketPrice;
				lastKnownMarketPrice = marketPrice;
				snapshot.CurrentUnitPrice = marketPrice;
				snapshot.TotalValue = marketPrice * snapshot.Quantity;

				snapshots.Add(snapshot);
				previousSnapshot = snapshot;
			}

			foreach (CalculatedSnapshot snapshot in snapshots)
			{
				snapshot.AverageCostPrice = Math.Round(snapshot.AverageCostPrice, Constants.NumberOfDecimals);
				snapshot.CurrentUnitPrice = Math.Round(snapshot.CurrentUnitPrice, Constants.NumberOfDecimals);
				snapshot.TotalInvested = Math.Round(snapshot.TotalInvested, Constants.NumberOfDecimals);
				snapshot.TotalValue = Math.Round(snapshot.TotalValue, Constants.NumberOfDecimals);
				snapshot.Quantity = Math.Round(snapshot.Quantity, Constants.NumberOfDecimals);
			}

			return snapshots;
		}

		private async Task AddActivitiesAsMarketDataIfNeeded(
			Dictionary<DateOnly, decimal> marketData,
			Dictionary<DateOnly, List<ActivityWithQuantityAndUnitPrice>> activitiesByDate,
			DateOnly minDate,
			DateOnly maxDate,
			Currency targetCurrency)
		{
			decimal lastKnownPrice = 0m;

			for (DateOnly date = minDate; date <= maxDate; date = date.AddDays(1))
			{
				if (!marketData.TryGetValue(date, out decimal value) || value == 0)
				{
					marketData[date] = lastKnownPrice;

					if (lastKnownPrice == 0 && activitiesByDate.TryGetValue(date, out List<ActivityWithQuantityAndUnitPrice>? activities))
					{
						foreach (ActivityWithQuantityAndUnitPrice activity in activities.OrderBy(x => (x.AdjustedUnitPrice.Amount != 0 ? x.AdjustedUnitPrice : x.UnitPrice).Amount))
						{
							Money priceToUse = activity.AdjustedUnitPrice.Amount != 0 ? activity.AdjustedUnitPrice : activity.UnitPrice;
							Money converted = await currencyExchange.ConvertMoney(
								priceToUse,
								targetCurrency,
								date).ConfigureAwait(false);
							decimal convertedAmount = converted.Amount;
							if (convertedAmount != 0)
							{
								marketData[date] = convertedAmount;
								lastKnownPrice = convertedAmount;
								break;
							}
						}
					}
				}

				if (marketData.TryGetValue(date, out decimal price) && price != 0)
				{
					lastKnownPrice = price;
				}
			}
		}

		private static async Task ApplyActivitiesForDateAsync(
			Dictionary<DateOnly, List<ActivityWithQuantityAndUnitPrice>> activitiesByDate,
			DateOnly date,
			CalculatedSnapshot snapshot,
			Currency targetCurrency,
			ICurrencyExchange currencyExchange)
		{
			if (!activitiesByDate.TryGetValue(date, out List<ActivityWithQuantityAndUnitPrice>? dayActivities))
			{
				return;
			}

			foreach (ActivityWithQuantityAndUnitPrice activity in dayActivities)
			{
				int sign = activity switch
				{
					BuyActivity or ReceiveActivity or GiftAssetActivity or StakingRewardActivity => 1,
					SellActivity or SendActivity => -1,
					_ => throw new InvalidOperationException($"Unsupported activity type: {activity.GetType().Name}"),
				};

				decimal totalFeesAndTaxes = await SumFeesAndTaxesAsync(activity, targetCurrency, date, currencyExchange).ConfigureAwait(false);

				if (sign == 1)
				{
					Money convertedTotal = await currencyExchange.ConvertMoney(
						activity.UnitPrice.Times(activity.Quantity),
						targetCurrency,
						date).ConfigureAwait(false);

					snapshot.TotalInvested += convertedTotal.Amount + totalFeesAndTaxes;
					snapshot.Quantity += activity.AdjustedQuantity;
					snapshot.AverageCostPrice = CalculateAverageCostPrice(snapshot);
				}
				else
				{
					decimal costBasisReduction = snapshot.AverageCostPrice * activity.AdjustedQuantity;
					snapshot.TotalInvested -= costBasisReduction + totalFeesAndTaxes;
					snapshot.Quantity -= activity.AdjustedQuantity;

					if (snapshot.Quantity <= 0)
					{
						snapshot.Quantity = 0;
						snapshot.TotalInvested = 0;
						snapshot.AverageCostPrice = 0;
					}
				}
			}
		}

		private static decimal CalculateAverageCostPrice(CalculatedSnapshot snapshot)
		{
			return snapshot.Quantity == 0 ? 0 : snapshot.TotalInvested / snapshot.Quantity;
		}

		private static decimal GetLastKnownMarketPrice(Dictionary<DateOnly, decimal> marketData, DateOnly minDate)
		{
			decimal lastKnownMarketPrice = 0;
			DateOnly lastKnownDate = default;

			foreach (KeyValuePair<DateOnly, decimal> marketDataPoint in marketData)
			{
				if (marketDataPoint.Key > minDate || marketDataPoint.Value == 0)
				{
					continue;
				}

				if (lastKnownMarketPrice == 0 || marketDataPoint.Key > lastKnownDate)
				{
					lastKnownMarketPrice = marketDataPoint.Value;
					lastKnownDate = marketDataPoint.Key;
				}
			}

			return lastKnownMarketPrice;
		}

		private static async Task<decimal> SumFeesAndTaxesAsync(
			ActivityWithQuantityAndUnitPrice activity,
			Currency targetCurrency,
			DateOnly date,
			ICurrencyExchange currencyExchange)
		{
			decimal totalFeesAndTaxes = 0;

			switch (activity)
			{
				case SellActivity sellActivity:
					totalFeesAndTaxes += await SumConvertedMoneyAsync(sellActivity.Fees, targetCurrency, date, currencyExchange).ConfigureAwait(false);
					totalFeesAndTaxes += await SumConvertedMoneyAsync(sellActivity.Taxes, targetCurrency, date, currencyExchange).ConfigureAwait(false);
					break;
				case BuyActivity buyActivity:
					totalFeesAndTaxes += await SumConvertedMoneyAsync(buyActivity.Fees, targetCurrency, date, currencyExchange).ConfigureAwait(false);
					totalFeesAndTaxes += await SumConvertedMoneyAsync(buyActivity.Taxes, targetCurrency, date, currencyExchange).ConfigureAwait(false);
					break;
				case ReceiveActivity receiveActivity:
					totalFeesAndTaxes += await SumConvertedMoneyAsync(receiveActivity.Fees, targetCurrency, date, currencyExchange).ConfigureAwait(false);
					break;
			}

			return totalFeesAndTaxes;
		}

		private static async Task<decimal> SumConvertedMoneyAsync(
			IEnumerable<Money>? amounts,
			Currency targetCurrency,
			DateOnly date,
			ICurrencyExchange currencyExchange)
		{
			if (amounts == null)
			{
				return 0;
			}

			decimal total = 0;
			foreach (Money amount in amounts)
			{
				Money convertedAmount = await currencyExchange.ConvertMoney(amount, targetCurrency, date).ConfigureAwait(false);
				total += convertedAmount.Amount;
			}

			return total;
		}
	}
}
