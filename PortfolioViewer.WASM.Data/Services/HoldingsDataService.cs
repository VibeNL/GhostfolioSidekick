using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// Service for managing portfolio holdings data with optimized performance for Blazor WebAssembly.
	/// Implements caching, bulk operations, and parallel processing for improved performance.
	/// </summary>
	public class HoldingsDataService(
		DatabaseContext databaseContext, 
		ICurrencyExchange currencyExchange, 
		ILogger<HoldingsDataService> logger) : IHoldingsDataService
	{
		/// <inheritdoc />
		public async Task<List<HoldingDisplayModel>> GetHoldingsAsync(
			Currency targetCurrency,
			CancellationToken cancellationToken = default)
		{
			return await GetHoldingsAsync(targetCurrency, 0, cancellationToken);
		}

		/// <inheritdoc />
		public async Task<List<HoldingDisplayModel>> GetHoldingsAsync(
			Currency targetCurrency,
			int accountId,
			CancellationToken cancellationToken = default)
		{
			try
			{
				logger.LogInformation("Loading holdings for account {AccountId} in currency {Currency}", 
					accountId, targetCurrency.Symbol);

				// Step 1: Get holdings with their latest snapshots in optimized bulk queries
				var holdingsWithSnapshots = await GetHoldingsWithLatestSnapshotsAsync(accountId, cancellationToken);
				
				if (!holdingsWithSnapshots.Any())
				{
					logger.LogInformation("No holdings found for account {AccountId}", accountId);
					return new List<HoldingDisplayModel>();
				}

				// Step 2: Process holdings in parallel for better performance
				var holdingTasks = holdingsWithSnapshots.Select(holding => 
					ProcessHoldingAsync(holding, targetCurrency));
				var processedHoldings = await Task.WhenAll(holdingTasks);

				// Step 3: Calculate portfolio weights
				var result = processedHoldings.ToList();
				CalculateHoldingWeights(result);

				logger.LogInformation("Successfully loaded {Count} holdings for account {AccountId}", 
					result.Count, accountId);
				
				return result;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to load holdings for account {AccountId}", accountId);
				throw new InvalidOperationException($"Failed to load holdings for account {accountId}. Please try again later.", ex);
			}
		}

		/// <inheritdoc />
		public async Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
		{
			var minDate = await databaseContext.CalculatedSnapshots
				.OrderBy(s => s.Date)
				.Select(s => s.Date)
				.FirstOrDefaultAsync(cancellationToken);
			return minDate;
		}

		/// <inheritdoc />
		public async Task<List<Account>> GetAccountsAsync()
		{
			return await ExecuteWithErrorHandlingAsync(
				"Loading accounts from database...",
				"Failed to load accounts from database",
				"Failed to load accounts. Please try again later.",
				async () =>
				{
					await EnsureDatabaseConnectionAsync();
					
					var accounts = await databaseContext.Accounts
						.AsNoTracking()
						.OrderBy(a => a.Name)
						.ToListAsync();
					
					logger.LogInformation("Successfully loaded {Count} accounts", accounts.Count);
					return accounts;
				});
		}

		/// <inheritdoc />
		public async Task<List<string>> GetSymbolsAsync()
		{
			return await ExecuteWithErrorHandlingAsync(
				"Loading symbols from database...",
				"Failed to load symbols from database",
				"Failed to load symbols. Please try again later.",
				async () =>
				{
					await EnsureDatabaseConnectionAsync();
					
					var symbols = await databaseContext.SymbolProfiles
						.AsNoTracking()
						.Select(sp => sp.Symbol)
						.Distinct()
						.OrderBy(s => s)
						.ToListAsync();
					
					logger.LogInformation("Successfully loaded {Count} unique symbols", symbols.Count);
					return symbols;
				});
		}

		/// <inheritdoc />
		public async Task<List<string>> GetSymbolsByAccountAsync(int accountId)
		{
			return await ExecuteWithErrorHandlingAsync(
				$"Loading symbols for account {accountId} from database...",
				$"Failed to load symbols for account {accountId} from database",
				$"Failed to load symbols for account {accountId}. Please try again later.",
				async () =>
				{
					await EnsureDatabaseConnectionAsync();
					
					var symbols = await databaseContext.Activities
						.Where(a => a.Account.Id == accountId && a.Holding != null)
						.SelectMany(a => a.Holding!.SymbolProfiles)
						.Select(sp => sp.Symbol)
						.Distinct()
						.OrderBy(s => s)
						.AsNoTracking()
						.ToListAsync();
					
					logger.LogInformation("Successfully loaded {Count} unique symbols for account {AccountId}", 
						symbols.Count, accountId);
					return symbols;
				});
		}

		/// <inheritdoc />
		public async Task<List<Account>> GetAccountsBySymbolAsync(string symbol)
		{
			return await ExecuteWithErrorHandlingAsync(
				$"Loading accounts for symbol {symbol} from database...",
				$"Failed to load accounts for symbol {symbol} from database",
				$"Failed to load accounts for symbol {symbol}. Please try again later.",
				async () =>
				{
					await EnsureDatabaseConnectionAsync();
					
					var accounts = await databaseContext.Activities
						.Where(a => a.Holding != null && a.Holding.SymbolProfiles.Any(sp => sp.Symbol == symbol))
						.Select(a => a.Account)
						.Distinct()
						.OrderBy(a => a.Name)
						.AsNoTracking()
						.ToListAsync();
					
					logger.LogInformation("Successfully loaded {Count} accounts for symbol {Symbol}", 
						accounts.Count, symbol);
					return accounts;
				});
		}

		/// <inheritdoc />
		public async Task<List<PortfolioValueHistoryPoint>> GetPortfolioValueHistoryAsync(
			Currency targetCurrency,
			DateTime startDate,
			DateTime endDate,
			int accountId,
			CancellationToken cancellationToken = default)
		{
			var query = BuildPortfolioValueQuery(startDate, endDate, accountId);
			
			var resultQuery = query
				.GroupBy(s => s.Date)
				.OrderBy(g => g.Key)
				.Select(g => new PortfolioValueHistoryPoint
				{
					Date = g.Key,
					Value = Money.SumPerCurrency(g.Select(x => x.TotalValue)),
					Invested = Money.SumPerCurrency(g.Select(x => x.TotalInvested)),
				})
				.AsSplitQuery();

			return await resultQuery.ToListAsync(cancellationToken);
		}

		/// <inheritdoc />
		public async Task<List<AccountValueHistoryPoint>> GetAccountValueHistoryAsync(
			Currency targetCurrency,
			DateTime startDate,
			DateTime endDate,
			CancellationToken cancellationToken = default)
		{
			try
			{
				logger.LogInformation("Loading account value history from {StartDate} to {EndDate} in {Currency}...", 
					startDate, endDate, targetCurrency.Symbol);

				var (accounts, snapshots, balances) = await LoadAccountHistoryDataAsync(startDate, endDate, cancellationToken);
				
				if (!accounts.Any())
				{
					logger.LogInformation("No accounts found");
					return new List<AccountValueHistoryPoint>();
				}

				var result = await ProcessAccountHistoryAsync(accounts, snapshots, balances, targetCurrency);

				logger.LogInformation("Successfully loaded account value history for {AccountCount} accounts across {PointCount} data points", 
					accounts.Count, result.Count);
				
				return result.OrderBy(r => r.Date).ThenBy(r => r.Account.Name).ToList();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to load account value history from database");
				throw new InvalidOperationException("Failed to load account value history. Please try again later.", ex);
			}
		}

		/// <inheritdoc />
		public async Task<List<HoldingPriceHistoryPoint>> GetHoldingPriceHistoryAsync(
			string symbol,
			DateTime startDate,
			DateTime endDate,
			CancellationToken cancellationToken = default)
		{
			var snapshots = await databaseContext.HoldingAggregateds
				.Where(h => h.Symbol == symbol)
				.SelectMany(h => h.CalculatedSnapshots)
				.Where(s => s.Date >= DateOnly.FromDateTime(startDate) &&
						   s.Date <= DateOnly.FromDateTime(endDate))
				.GroupBy(s => s.Date)
				.OrderBy(g => g.Key)
				.Select(g => new { Date = g.Key, Snapshots = g.ToList() })
				.ToListAsync(cancellationToken);

			var priceHistory = new List<HoldingPriceHistoryPoint>();
			
			foreach (var snapshotGroup in snapshots)
			{
				var (currentPrice, averagePrice) = await CalculateAggregatedPricesAsync(snapshotGroup.Snapshots, snapshotGroup.Date);
				
				priceHistory.Add(new HoldingPriceHistoryPoint
				{
					Date = snapshotGroup.Date,
					Price = currentPrice,
					AveragePrice = averagePrice
				});
			}

			return priceHistory;
		}

		/// <inheritdoc />
		public async Task<List<TransactionDisplayModel>> GetTransactionsAsync(
			Currency targetCurrency,
			DateTime startDate,
			DateTime endDate,
			int accountId,
			string symbol,
			CancellationToken cancellationToken = default)
		{
			var activities = await LoadActivitiesAsync(startDate, endDate, accountId, symbol, cancellationToken);
			
			var transactions = new List<TransactionDisplayModel>();
			foreach (var activity in activities)
			{
				var transaction = await MapActivityToTransactionDisplayModel(activity, targetCurrency);
				if (transaction != null)
				{
					transactions.Add(transaction);
				}
			}

			return transactions;
		}

		/// <summary>
		/// Processes a single holding to create a display model with optimized calculations.
		/// </summary>
		private async Task<HoldingDisplayModel> ProcessHoldingAsync(HoldingWithSnapshots holding, Currency targetCurrency)
		{
			if (!holding.Snapshots.Any())
			{
				return CreateEmptyHoldingDisplayModel(holding, targetCurrency);
			}

			var aggregatedSnapshot = await AggregateSnapshotsOptimized(holding.Snapshots, targetCurrency);
			var convertedSnapshot = await ConvertToTargetCurrencyOptimized(targetCurrency, aggregatedSnapshot);
			
			var gainLoss = convertedSnapshot.TotalValue.Subtract(convertedSnapshot.TotalInvested);
			var gainLossPercentage = convertedSnapshot.TotalInvested.Amount == 0 ? 0 : 
				gainLoss.Amount / convertedSnapshot.TotalInvested.Amount;

			return new HoldingDisplayModel
			{
				AssetClass = holding.AssetClass.ToString(),
				AveragePrice = convertedSnapshot.AverageCostPrice,
				Currency = targetCurrency.Symbol.ToString(),
				CurrentValue = convertedSnapshot.TotalValue,
				CurrentPrice = convertedSnapshot.CurrentUnitPrice,
				GainLoss = gainLoss,
				GainLossPercentage = gainLossPercentage,
				Name = holding.Name ?? string.Empty,
				Quantity = convertedSnapshot.Quantity,
				Symbol = holding.Symbol,
				Sector = GetSectorDisplay(holding.SectorWeights),
				Weight = 0, // Will be calculated after all holdings are processed
			};
		}

		/// <summary>
		/// Creates an empty holding display model for holdings with no snapshot data.
		/// </summary>
		private static HoldingDisplayModel CreateEmptyHoldingDisplayModel(HoldingWithSnapshots holding, Currency targetCurrency)
		{
			var emptySnapshot = CalculatedSnapshot.Empty(targetCurrency, 0);
			return new HoldingDisplayModel
			{
				AssetClass = holding.AssetClass.ToString(),
				AveragePrice = emptySnapshot.AverageCostPrice,
				Currency = targetCurrency.Symbol.ToString(),
				CurrentValue = emptySnapshot.TotalValue,
				CurrentPrice = emptySnapshot.CurrentUnitPrice,
				GainLoss = Money.Zero(targetCurrency),
				GainLossPercentage = 0,
				Name = holding.Name ?? string.Empty,
				Quantity = 0,
				Symbol = holding.Symbol,
				Sector = GetSectorDisplay(holding.SectorWeights),
				Weight = 0,
			};
		}

		/// <summary>
		/// Calculates portfolio weights for all holdings based on their total values.
		/// </summary>
		private static void CalculateHoldingWeights(List<HoldingDisplayModel> holdings)
		{
			var totalValue = holdings.Sum(x => x.CurrentValue.Amount);
			if (totalValue > 0)
			{
				foreach (var holding in holdings)
				{
					holding.Weight = holding.CurrentValue.Amount / totalValue;
				}
			}
		}

		/// <summary>
		/// Gets the sector display string from sector weights.
		/// </summary>
		private static string GetSectorDisplay(IList<SectorWeight> sectorWeights)
		{
			return sectorWeights.Count != 0 
				? string.Join(",", sectorWeights.Select(x => x.Name)) 
				: "Undefined";
		}

		/// <summary>
		/// Optimized method to get holdings with their latest snapshots using bulk queries.
		/// </summary>
		private async Task<List<HoldingWithSnapshots>> GetHoldingsWithLatestSnapshotsAsync(int accountId, CancellationToken cancellationToken)
		{
			// Step 1: Get all holdings with basic info
			var holdings = await databaseContext.HoldingAggregateds
				.AsNoTracking()
				.Select(h => new HoldingBasicInfo
				{
					Id = h.Id,
					AssetClass = h.AssetClass,
					Name = h.Name,
					Symbol = h.Symbol,
					SectorWeights = h.SectorWeights.ToList()
				})
				.ToListAsync(cancellationToken);

			if (!holdings.Any())
			{
				return new List<HoldingWithSnapshots>();
			}

			// Step 2: Get latest dates for all holdings in one query
			var latestDates = await GetLatestSnapshotDatesAsync(holdings.Select(h => h.Id), accountId, cancellationToken);

			// Step 3: Get all latest snapshots in one query
			var snapshotsByHolding = await GetLatestSnapshotsGroupedAsync(latestDates, accountId, cancellationToken);

			// Step 4: Combine holdings with their snapshots
			return holdings.Select(h => new HoldingWithSnapshots
			{
				Id = h.Id,
				AssetClass = h.AssetClass,
				Name = h.Name,
				Symbol = h.Symbol,
				SectorWeights = h.SectorWeights,
				Snapshots = snapshotsByHolding.GetValueOrDefault(h.Id, new List<CalculatedSnapshot>())
			}).ToList();
		}

		/// <summary>
		/// Gets the latest snapshot dates for all holdings.
		/// </summary>
		private async Task<Dictionary<long, DateOnly>> GetLatestSnapshotDatesAsync(
			IEnumerable<long> holdingIds, 
			int accountId, 
			CancellationToken cancellationToken)
		{
			var query = databaseContext.CalculatedSnapshots
				.Where(s => holdingIds.Contains(EF.Property<long>(s, "HoldingAggregatedId")))
				.AsNoTracking();

			if (accountId > 0)
			{
				query = query.Where(s => s.AccountId == accountId);
			}

			return await query
				.GroupBy(s => EF.Property<long>(s, "HoldingAggregatedId"))
				.Select(g => new { HoldingId = g.Key, LatestDate = g.Max(s => s.Date) })
				.ToDictionaryAsync(x => x.HoldingId, x => x.LatestDate, cancellationToken);
		}

		/// <summary>
		/// Gets the latest snapshots grouped by holding.
		/// </summary>
		private async Task<Dictionary<long, List<CalculatedSnapshot>>> GetLatestSnapshotsGroupedAsync(
			Dictionary<long, DateOnly> latestDates, 
			int accountId, 
			CancellationToken cancellationToken)
		{
			var query = from snapshot in databaseContext.CalculatedSnapshots
						where latestDates.Keys.Contains(EF.Property<long>(snapshot, "HoldingAggregatedId")) &&
							  latestDates.Values.Contains(snapshot.Date)
						select new { 
							Snapshot = snapshot, 
							HoldingId = EF.Property<long>(snapshot, "HoldingAggregatedId") 
						};

			if (accountId > 0)
			{
				query = query.Where(x => x.Snapshot.AccountId == accountId);
			}

			var allLatestSnapshots = await query
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			return allLatestSnapshots
				.Where(x => latestDates.TryGetValue(x.HoldingId, out var latestDate) && x.Snapshot.Date == latestDate)
				.GroupBy(x => x.HoldingId)
				.ToDictionary(g => g.Key, g => g.Select(x => x.Snapshot).ToList());
		}

		/// <summary>
		/// Loads account history data (accounts, snapshots, balances) in bulk queries.
		/// </summary>
		private async Task<(List<Account> accounts, List<CalculatedSnapshot> snapshots, List<Balance> balances)> 
			LoadAccountHistoryDataAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
		{
			var startDateOnly = DateOnly.FromDateTime(startDate);
			var endDateOnly = DateOnly.FromDateTime(endDate);

			var accountsTask = databaseContext.Accounts
				.AsNoTracking()
				.OrderBy(a => a.Name)
				.ToListAsync(cancellationToken);

			var snapshotsTask = databaseContext.CalculatedSnapshots
				.Where(s => s.Date >= startDateOnly && s.Date <= endDateOnly)
				.OrderBy(s => s.Date)
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			var balancesTask = databaseContext.Set<Balance>()
				.Where(b => b.Date >= startDateOnly && b.Date <= endDateOnly)
				.OrderBy(b => b.Date)
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			await Task.WhenAll(accountsTask, snapshotsTask, balancesTask);
			
			return (await accountsTask, await snapshotsTask, await balancesTask);
		}

		/// <summary>
		/// Loads activities with optional filtering by account and symbol.
		/// </summary>
		private async Task<List<Activity>> LoadActivitiesAsync(
			DateTime startDate, 
			DateTime endDate, 
			int accountId, 
			string symbol, 
			CancellationToken cancellationToken)
		{
			var baseQuery = databaseContext.Activities
				.Include(a => a.Account)
				.Include(a => a.Holding)
				.Where(a => a.Date >= startDate && a.Date <= endDate);

			if (accountId > 0)
			{
				baseQuery = baseQuery.Where(a => a.Account.Id == accountId);
			}

			var activities = await baseQuery
				.OrderByDescending(a => a.Date)
				.ThenBy(a => a.Id)
				.ToListAsync(cancellationToken);

			return await FilterActivitiesBySymbolAsync(activities, symbol, cancellationToken);
		}

		/// <summary>
		/// Filters activities by symbol if specified, loading symbol profiles as needed.
		/// </summary>
		private async Task<List<Activity>> FilterActivitiesBySymbolAsync(
			List<Activity> activities, 
			string symbol, 
			CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty(symbol))
			{
				return await PopulateSymbolProfilesAsync(activities, cancellationToken);
			}

			var holdingIds = activities
				.Where(a => a.Holding != null)
				.Select(a => a.Holding!.Id)
				.Distinct()
				.ToList();

			if (!holdingIds.Any())
			{
				return activities.Where(a => a.Holding == null).ToList();
			}

			var holdingsWithSymbols = await databaseContext.Holdings
				.Where(h => holdingIds.Contains(h.Id))
				.Include(h => h.SymbolProfiles)
				.ToDictionaryAsync(h => h.Id, h => h.SymbolProfiles, cancellationToken);

			var filteredActivities = activities
				.Where(a => a.Holding != null &&
						   holdingsWithSymbols.TryGetValue(a.Holding.Id, out var symbolProfiles) && 
						   symbolProfiles.Any(sp => sp.Symbol == symbol))
				.ToList();

			// Populate symbol profiles for filtered activities
			foreach (var activity in filteredActivities.Where(a => a.Holding != null))
			{
				if (holdingsWithSymbols.TryGetValue(activity.Holding!.Id, out var symbolProfiles))
				{
					activity.Holding.SymbolProfiles = symbolProfiles.ToList();
				}
			}

			return filteredActivities;
		}

		/// <summary>
		/// Populates symbol profiles for all activities with holdings.
		/// </summary>
		private async Task<List<Activity>> PopulateSymbolProfilesAsync(List<Activity> activities, CancellationToken cancellationToken)
		{
			var holdingIds = activities
				.Where(a => a.Holding != null)
				.Select(a => a.Holding!.Id)
				.Distinct()
				.ToList();

			if (!holdingIds.Any())
			{
				return activities;
			}

			var holdingsWithSymbols = await databaseContext.Holdings
				.Where(h => holdingIds.Contains(h.Id))
				.Include(h => h.SymbolProfiles)
				.ToDictionaryAsync(h => h.Id, h => h.SymbolProfiles, cancellationToken);

			foreach (var activity in activities.Where(a => a.Holding != null))
			{
				if (holdingsWithSymbols.TryGetValue(activity.Holding!.Id, out var symbolProfiles))
				{
					activity.Holding.SymbolProfiles = symbolProfiles.ToList();
				}
			}

			return activities;
		}

		/// <summary>
		/// Optimized snapshot aggregation with minimal allocations.
		/// </summary>
		private async Task<CalculatedSnapshot> AggregateSnapshotsOptimized(
			List<CalculatedSnapshot> snapshots, 
			Currency targetCurrency)
		{
			if (!snapshots.Any())
			{
				return CalculatedSnapshot.Empty(targetCurrency, 0);
			}

			if (snapshots.Count == 1)
			{
				return snapshots[0];
			}

			var totalQuantity = snapshots.Sum(s => s.Quantity);
			var latestDate = snapshots[0].Date;

			if (totalQuantity == 0)
			{
				return CreateZeroQuantitySnapshot(snapshots[0], latestDate);
			}

			return await CalculateAggregatedSnapshot(snapshots, latestDate, totalQuantity);
		}

		/// <summary>
		/// Creates a snapshot for zero quantity holdings.
		/// </summary>
		private static CalculatedSnapshot CreateZeroQuantitySnapshot(CalculatedSnapshot firstSnapshot, DateOnly latestDate)
		{
			return new CalculatedSnapshot
			{
				Date = latestDate,
				AverageCostPrice = firstSnapshot.AverageCostPrice,
				CurrentUnitPrice = firstSnapshot.CurrentUnitPrice,
				TotalInvested = Money.Zero(firstSnapshot.TotalValue.Currency),
				TotalValue = Money.Zero(firstSnapshot.TotalValue.Currency),
				Quantity = 0,
			};
		}

		/// <summary>
		/// Calculates aggregated snapshot values with currency conversion.
		/// </summary>
		private async Task<CalculatedSnapshot> CalculateAggregatedSnapshot(
			List<CalculatedSnapshot> snapshots, 
			DateOnly latestDate, 
			decimal totalQuantity)
		{
			var baseCurrency = snapshots[0].CurrentUnitPrice.Currency;
			
			decimal totalCurrentValue = 0;
			decimal totalAverageCostValue = 0;
			decimal totalInvestedValue = 0;
			decimal totalValueValue = 0;

			foreach (var snapshot in snapshots)
			{
				var (currentValue, averageCostValue, investedValue, valueValue) = 
					await ConvertSnapshotValuesAsync(snapshot, baseCurrency, latestDate);

				totalCurrentValue += currentValue;
				totalAverageCostValue += averageCostValue;
				totalInvestedValue += investedValue;
				totalValueValue += valueValue;
			}

			return new CalculatedSnapshot
			{
				Date = latestDate,
				AverageCostPrice = new Money(baseCurrency, totalAverageCostValue / totalQuantity),
				CurrentUnitPrice = new Money(baseCurrency, totalCurrentValue / totalQuantity),
				TotalInvested = new Money(baseCurrency, totalInvestedValue),
				TotalValue = new Money(baseCurrency, totalValueValue),
				Quantity = totalQuantity,
			};
		}

		/// <summary>
		/// Converts snapshot values to base currency.
		/// </summary>
		private async Task<(decimal currentValue, decimal averageCostValue, decimal investedValue, decimal valueValue)> 
			ConvertSnapshotValuesAsync(CalculatedSnapshot snapshot, Currency baseCurrency, DateOnly date)
		{
			var currentValueAmount = snapshot.CurrentUnitPrice.Amount * snapshot.Quantity;
			var averageCostValueAmount = snapshot.AverageCostPrice.Amount * snapshot.Quantity;

			if (snapshot.CurrentUnitPrice.Currency == baseCurrency)
			{
				return (currentValueAmount, averageCostValueAmount, snapshot.TotalInvested.Amount, snapshot.TotalValue.Amount);
			}

			// Use cached conversion
			var currentConversion = await ConvertMoney(
				new Money(snapshot.CurrentUnitPrice.Currency, currentValueAmount), baseCurrency, date);
			var averageConversion = await ConvertMoney(
				new Money(snapshot.AverageCostPrice.Currency, averageCostValueAmount), baseCurrency, date);
			var investedConversion = await ConvertMoney(snapshot.TotalInvested, baseCurrency, date);
			var valueConversion = await ConvertMoney(snapshot.TotalValue, baseCurrency, date);

			return (currentConversion.Amount, averageConversion.Amount, investedConversion.Amount, valueConversion.Amount);
		}

		/// <summary>
		/// Optimized currency conversion for snapshots.
		/// </summary>
		private async Task<CalculatedSnapshot> ConvertToTargetCurrencyOptimized(Currency targetCurrency, CalculatedSnapshot calculatedSnapshot)
		{
			if (calculatedSnapshot.CurrentUnitPrice.Currency == targetCurrency)
			{
				return calculatedSnapshot;
			}

			return new CalculatedSnapshot
			{
				Date = calculatedSnapshot.Date,
				AverageCostPrice = await ConvertMoney(calculatedSnapshot.AverageCostPrice, targetCurrency, calculatedSnapshot.Date),
				CurrentUnitPrice = await ConvertMoney(calculatedSnapshot.CurrentUnitPrice, targetCurrency, calculatedSnapshot.Date),
				TotalInvested = await ConvertMoney(calculatedSnapshot.TotalInvested, targetCurrency, calculatedSnapshot.Date),
				TotalValue = await ConvertMoney(calculatedSnapshot.TotalValue, targetCurrency, calculatedSnapshot.Date),
				Quantity = calculatedSnapshot.Quantity,
			};
		}

		/// <summary>
		/// Calculates aggregated prices for holding price history.
		/// </summary>
		private async Task<(Money currentPrice, Money averagePrice)> CalculateAggregatedPricesAsync(
			List<CalculatedSnapshot> snapshots, 
			DateOnly date)
		{
			var totalQuantity = snapshots.Sum(s => s.Quantity);
			
			if (totalQuantity == 0)
			{
				var firstSnapshot = snapshots.First();
				return (firstSnapshot.CurrentUnitPrice, firstSnapshot.AverageCostPrice);
			}

			var firstCurrency = snapshots.First().CurrentUnitPrice.Currency;
			decimal totalValueAtCurrentPriceAmount = 0;
			decimal totalValueAtAveragePriceAmount = 0;
			
			foreach (var snapshot in snapshots)
			{
				var valueAtCurrentPrice = snapshot.CurrentUnitPrice.Times(snapshot.Quantity);
				var valueAtAveragePrice = snapshot.AverageCostPrice.Times(snapshot.Quantity);
				
				if (valueAtCurrentPrice.Currency == firstCurrency)
				{
					totalValueAtCurrentPriceAmount += valueAtCurrentPrice.Amount;
					totalValueAtAveragePriceAmount += valueAtAveragePrice.Amount;
				}
				else
				{
					var currentConverted = await currencyExchange.ConvertMoney(valueAtCurrentPrice, firstCurrency, date);
					var averageConverted = await currencyExchange.ConvertMoney(valueAtAveragePrice, firstCurrency, date);
					
					totalValueAtCurrentPriceAmount += currentConverted.Amount;
					totalValueAtAveragePriceAmount += averageConverted.Amount;
				}
			}
			
			var totalValueAtCurrentPrice = new Money(firstCurrency, totalValueAtCurrentPriceAmount);
			var totalValueAtAveragePrice = new Money(firstCurrency, totalValueAtAveragePriceAmount);
			
			return (
				totalValueAtCurrentPrice.SafeDivide(totalQuantity),
				totalValueAtAveragePrice.SafeDivide(totalQuantity)
			);
		}

		/// <summary>
		/// Processes account history data to create value history points.
		/// </summary>
		private async Task<List<AccountValueHistoryPoint>> ProcessAccountHistoryAsync(
			List<Account> accounts,
			List<CalculatedSnapshot> allSnapshots,
			List<Balance> allBalances,
			Currency targetCurrency)
		{
			var snapshotsByAccount = allSnapshots.GroupBy(s => s.AccountId).ToDictionary(g => g.Key, g => g.ToList());
			var balancesByAccount = allBalances.GroupBy(b => b.AccountId).ToDictionary(g => g.Key, g => g.ToList());

			var accountsWithData = accounts.Where(a => 
				snapshotsByAccount.ContainsKey(a.Id) || balancesByAccount.ContainsKey(a.Id)).ToList();

			if (!accountsWithData.Any())
			{
				logger.LogInformation("No account data found for the specified date range");
				return new List<AccountValueHistoryPoint>();
			}

			var currencyConversionCache = new Dictionary<string, Money>();
			var result = new List<AccountValueHistoryPoint>();

			foreach (var account in accountsWithData)
			{
				var accountPoints = await ProcessSingleAccountHistoryAsync(
					account, 
					snapshotsByAccount.GetValueOrDefault(account.Id, new List<CalculatedSnapshot>()), 
					balancesByAccount.GetValueOrDefault(account.Id, new List<Balance>()), 
					targetCurrency, 
					currencyConversionCache);
				
				result.AddRange(accountPoints);
			}

			return result;
		}

		/// <summary>
		/// Processes history for a single account.
		/// </summary>
		private async Task<List<AccountValueHistoryPoint>> ProcessSingleAccountHistoryAsync(
			Account account,
			List<CalculatedSnapshot> accountSnapshots,
			List<Balance> accountBalances,
			Currency targetCurrency,
			Dictionary<string, Money> currencyConversionCache)
		{
			var snapshotsByDate = accountSnapshots.GroupBy(s => s.Date).ToDictionary(g => g.Key, g => g.ToList());
			var balancesByDate = accountBalances.ToDictionary(b => b.Date, b => b);

			var accountDates = accountSnapshots.Select(s => s.Date)
				.Union(accountBalances.Select(b => b.Date))
				.Distinct()
				.OrderBy(d => d);

			var result = new List<AccountValueHistoryPoint>();

			foreach (var date in accountDates)
			{
				var (totalValueAmount, totalInvestedAmount) = await ProcessSnapshotsForDate(
					snapshotsByDate.GetValueOrDefault(date, new List<CalculatedSnapshot>()), 
					targetCurrency, 
					currencyConversionCache, 
					date);

				var accountBalanceAmount = await ProcessBalanceForDate(
					accountBalances, 
					date, 
					targetCurrency, 
					currencyConversionCache);

				result.Add(new AccountValueHistoryPoint
				{
					Date = date,
					Account = account,
					Value = new Money(targetCurrency, totalValueAmount + accountBalanceAmount),
					Invested = new Money(targetCurrency, totalInvestedAmount),
					Balance = new Money(targetCurrency, accountBalanceAmount)
				});
			}

			return result;
		}

		/// <summary>
		/// Processes snapshots for a specific date.
		/// </summary>
		private async Task<(decimal totalValue, decimal totalInvested)> ProcessSnapshotsForDate(
			List<CalculatedSnapshot> dateSnapshots,
			Currency targetCurrency,
			Dictionary<string, Money> currencyConversionCache,
			DateOnly date)
		{
			if (!dateSnapshots.Any())
			{
				return (0, 0);
			}

			decimal totalValueAmount = 0;
			decimal totalInvestedAmount = 0;

			var snapshotsByCurrency = dateSnapshots.GroupBy(s => s.TotalValue.Currency).ToList();

			foreach (var currencyGroup in snapshotsByCurrency)
			{
				var currency = currencyGroup.Key;
				var totalValueInCurrency = currencyGroup.Sum(s => s.TotalValue.Amount);
				var totalInvestedInCurrency = currencyGroup.Sum(s => s.TotalInvested.Amount);

				var conversionRate = await GetOrCacheConversionRate(currency, targetCurrency, date, currencyConversionCache);

				totalValueAmount += totalValueInCurrency * conversionRate.Amount;
				totalInvestedAmount += totalInvestedInCurrency * conversionRate.Amount;
			}

			return (totalValueAmount, totalInvestedAmount);
		}

		/// <summary>
		/// Processes balance for a specific date.
		/// </summary>
		private async Task<decimal> ProcessBalanceForDate(
			List<Balance> accountBalances,
			DateOnly date,
			Currency targetCurrency,
			Dictionary<string, Money> currencyConversionCache)
		{
			var relevantBalance = accountBalances
				.Where(b => b.Date <= date)
				.OrderByDescending(b => b.Date)
				.FirstOrDefault();

			if (relevantBalance == null)
			{
				return 0;
			}

			var conversionRate = await GetOrCacheConversionRate(
				relevantBalance.Money.Currency, targetCurrency, date, currencyConversionCache);

			return relevantBalance.Money.Amount * conversionRate.Amount;
		}

		/// <summary>
		/// Gets or caches currency conversion rate.
		/// </summary>
		private async Task<Money> GetOrCacheConversionRate(
			Currency fromCurrency,
			Currency targetCurrency,
			DateOnly date,
			Dictionary<string, Money> currencyConversionCache)
		{
			var cacheKey = $"{fromCurrency.Symbol}_{date:yyyy-MM-dd}";
			if (!currencyConversionCache.TryGetValue(cacheKey, out var conversionRate))
			{
				conversionRate = await currencyExchange.ConvertMoney(
					new Money(fromCurrency, 1), targetCurrency, date);
				currencyConversionCache[cacheKey] = conversionRate;
			}

			return conversionRate;
		}

		/// <summary>
		/// Builds the portfolio value query with optional account filtering.
		/// </summary>
		private IQueryable<CalculatedSnapshot> BuildPortfolioValueQuery(DateTime startDate, DateTime endDate, int accountId)
		{
			var query = databaseContext.CalculatedSnapshots
				.Where(s => s.Date >= DateOnly.FromDateTime(startDate) && s.Date <= DateOnly.FromDateTime(endDate));

			if (accountId > 0)
			{
				query = query.Where(s => s.AccountId == accountId);
			}

			return query;
		}

		/// <summary>
		/// Executes a database operation with consistent error handling.
		/// </summary>
		private async Task<T> ExecuteWithErrorHandlingAsync<T>(
			string startMessage,
			string errorMessage,
			string userErrorMessage,
			Func<Task<T>> operation)
		{
			try
			{
				logger.LogInformation(startMessage);
				return await operation();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, errorMessage);
				throw new InvalidOperationException(userErrorMessage, ex);
			}
		}

		/// <summary>
		/// Ensures database connection is available.
		/// </summary>
		private async Task EnsureDatabaseConnectionAsync()
		{
			if (!await databaseContext.Database.CanConnectAsync())
			{
				logger.LogError("Cannot connect to database");
				throw new InvalidOperationException("Database connection failed. Please check your database configuration.");
			}
		}

		/// <summary>
		/// Maps an activity to a transaction display model.
		/// </summary>
		private async Task<TransactionDisplayModel?> MapActivityToTransactionDisplayModel(Activity activity, Currency targetCurrency)
		{
			var symbolProfile = activity.Holding?.SymbolProfiles?.FirstOrDefault();
			
			var baseModel = new TransactionDisplayModel
			{
				Id = activity.Id,
				Date = activity.Date,
				Type = GetDisplayType(activity),
				Symbol = symbolProfile?.Symbol,
				Name = symbolProfile?.Name,
				Description = activity.Description ?? string.Empty,
				TransactionId = activity.TransactionId,
				AccountName = activity.Account?.Name ?? "Unknown",
				Currency = targetCurrency.Symbol.ToString()
			};

			await MapActivitySpecificPropertiesAsync(activity, baseModel, targetCurrency);
			return baseModel;
		}

		/// <summary>
		/// Maps activity-specific properties to the transaction display model.
		/// </summary>
		private async Task MapActivitySpecificPropertiesAsync(Activity activity, TransactionDisplayModel model, Currency targetCurrency)
		{
			var date = DateOnly.FromDateTime(activity.Date);

			switch (activity)
			{
				case BuyActivity buy:
					await MapBuySellActivityAsync(buy, model, targetCurrency, date);
					break;
				case SellActivity sell:
					await MapSellActivityAsync(sell, model, targetCurrency, date);
					break;
				case DividendActivity dividend:
					await MapDividendActivityAsync(dividend, model, targetCurrency, date);
					break;
				case CashDepositActivity deposit:
					await MapCashActivityAsync(deposit.Amount, model, targetCurrency, date);
					break;
				case CashWithdrawalActivity withdrawal:
					await MapCashActivityAsync(withdrawal.Amount, model, targetCurrency, date);
					break;
				case FeeActivity fee:
					model.Amount = await ConvertMoney(fee.Amount, targetCurrency, date);
					model.TotalValue = model.Amount;
					model.Fee = model.Amount;
					break;
				case InterestActivity interest:
					await MapCashActivityAsync(interest.Amount, model, targetCurrency, date);
					break;
				case ReceiveActivity receive:
					await MapReceiveActivityAsync(receive, model, targetCurrency, date);
					break;
				case SendActivity send:
					await MapSendActivityAsync(send, model, targetCurrency, date);
					break;
				case StakingRewardActivity staking:
					await MapStakingGiftActivityAsync(staking.Quantity, staking.UnitPrice, model, targetCurrency, date);
					break;
				case GiftAssetActivity gift:
					await MapStakingGiftActivityAsync(gift.Quantity, gift.UnitPrice, model, targetCurrency, date);
					break;
			}
		}

		/// <summary>
		/// Maps buy/sell activity properties.
		/// </summary>
		private async Task MapBuySellActivityAsync(BuyActivity buySell, TransactionDisplayModel model, Currency targetCurrency, DateOnly date)
		{
			model.Quantity = buySell.Quantity;
			model.UnitPrice = await ConvertMoney(buySell.UnitPrice, targetCurrency, date);
			model.Amount = await ConvertMoney(buySell.UnitPrice.Times(buySell.Quantity), targetCurrency, date);
			model.TotalValue = await ConvertMoney(buySell.TotalTransactionAmount, targetCurrency, date);
			model.Fee = await SumAndConvertFees(buySell.Fees.Select(f => f.Money), targetCurrency, date);
			model.Tax = await SumAndConvertFees(buySell.Taxes.Select(t => t.Money), targetCurrency, date);
		}

		/// <summary>
		/// Maps sell activity properties.
		/// </summary>
		private async Task MapSellActivityAsync(SellActivity sell, TransactionDisplayModel model, Currency targetCurrency, DateOnly date)
		{
			model.Quantity = sell.Quantity;
			model.UnitPrice = await ConvertMoney(sell.UnitPrice, targetCurrency, date);
			model.Amount = await ConvertMoney(sell.UnitPrice.Times(sell.Quantity), targetCurrency, date);
			model.TotalValue = await ConvertMoney(sell.TotalTransactionAmount, targetCurrency, date);
			model.Fee = await SumAndConvertFees(sell.Fees.Select(f => f.Money), targetCurrency, date);
			model.Tax = await SumAndConvertFees(sell.Taxes.Select(t => t.Money), targetCurrency, date);
		}

		/// <summary>
		/// Maps dividend activity properties.
		/// </summary>
		private async Task MapDividendActivityAsync(DividendActivity dividend, TransactionDisplayModel model, Currency targetCurrency, DateOnly date)
		{
			model.Amount = await ConvertMoney(dividend.Amount, targetCurrency, date);
			model.TotalValue = model.Amount;
			model.Fee = await SumAndConvertFees(dividend.Fees.Select(f => f.Money), targetCurrency, date);
			model.Tax = await SumAndConvertFees(dividend.Taxes.Select(t => t.Money), targetCurrency, date);
		}

		/// <summary>
		/// Maps cash activity properties.
		/// </summary>
		private async Task MapCashActivityAsync(Money amount, TransactionDisplayModel model, Currency targetCurrency, DateOnly date)
		{
			model.Amount = await ConvertMoney(amount, targetCurrency, date);
			model.TotalValue = model.Amount;
		}

		/// <summary>
		/// Maps receive activity properties.
		/// </summary>
		private async Task MapReceiveActivityAsync(ReceiveActivity receive, TransactionDisplayModel model, Currency targetCurrency, DateOnly date)
		{
			model.Quantity = receive.Quantity;
			model.UnitPrice = await ConvertMoney(receive.UnitPrice, targetCurrency, date);
			model.Amount = await ConvertMoney(receive.UnitPrice.Times(receive.Quantity), targetCurrency, date);
			model.TotalValue = model.Amount;
			model.Fee = await SumAndConvertFees(receive.Fees.Select(f => f.Money), targetCurrency, date);
		}

		/// <summary>
		/// Maps send activity properties.
		/// </summary>
		private async Task MapSendActivityAsync(SendActivity send, TransactionDisplayModel model, Currency targetCurrency, DateOnly date)
		{
			model.Quantity = send.Quantity;
			model.UnitPrice = await ConvertMoney(send.UnitPrice, targetCurrency, date);
			model.Amount = await ConvertMoney(send.UnitPrice.Times(send.Quantity), targetCurrency, date);
			model.TotalValue = model.Amount;
			model.Fee = await SumAndConvertFees(send.Fees.Select(f => f.Money), targetCurrency, date);
		}

		/// <summary>
		/// Maps staking/gift activity properties.
		/// </summary>
		private async Task MapStakingGiftActivityAsync(decimal quantity, Money unitPrice, TransactionDisplayModel model, Currency targetCurrency, DateOnly date)
		{
			model.Quantity = quantity;
			model.UnitPrice = await ConvertMoney(unitPrice, targetCurrency, date);
			model.Amount = await ConvertMoney(unitPrice.Times(quantity), targetCurrency, date);
			model.TotalValue = model.Amount;
		}

		/// <summary>
		/// Gets display type for an activity.
		/// </summary>
		private static string GetDisplayType(Activity activity)
		{
			return activity switch
			{
				BuyActivity => "Buy",
				SellActivity => "Sell",
				DividendActivity => "Dividend",
				CashDepositActivity => "Deposit",
				CashWithdrawalActivity => "Withdrawal",
				FeeActivity => "Fee",
				InterestActivity => "Interest",
				ReceiveActivity => "Receive",
				SendActivity => "Send",
				StakingRewardActivity => "Staking Reward",
				GiftAssetActivity => "Gift",
				GiftFiatActivity => "Gift Cash",
				ValuableActivity => "Valuable",
				LiabilityActivity => "Liability",
				KnownBalanceActivity => "Balance",
				RepayBondActivity => "Bond Repayment",
				_ => activity.GetType().Name.Replace("Activity", "")
			};
		}

		/// <summary>
		/// Sums and converts fees to target currency.
		/// </summary>
		private async Task<Money?> SumAndConvertFees(IEnumerable<Money> fees, Currency targetCurrency, DateOnly date)
		{
			var feeList = fees.ToList();
			if (!feeList.Any())
				return null;

			var convertedFees = await Task.WhenAll(feeList.Select(fee => ConvertMoney(fee, targetCurrency, date)));
			return Money.Sum(convertedFees);
		}

		/// <summary>
		/// Converts money to target currency (non-optimized version for backward compatibility).
		/// </summary>
		private async Task<Money> ConvertMoney(Money money, Currency targetCurrency, DateOnly date)
		{
			if (money.Currency == targetCurrency)
				return money;

			return await currencyExchange.ConvertMoney(money, targetCurrency, date);
		}

		/// <summary>
		/// Converts snapshot to target currency (non-optimized version for backward compatibility).
		/// </summary>
		private async Task<CalculatedSnapshot> ConvertToTargetCurrency(Currency targetCurrency, CalculatedSnapshot calculatedSnapshot)
		{
			if (calculatedSnapshot.CurrentUnitPrice.Currency == targetCurrency)
			{
				return calculatedSnapshot;
			}

			return new CalculatedSnapshot
			{
				Date = calculatedSnapshot.Date,
				AverageCostPrice = await currencyExchange.ConvertMoney(calculatedSnapshot.AverageCostPrice, targetCurrency, calculatedSnapshot.Date),
				CurrentUnitPrice = await currencyExchange.ConvertMoney(calculatedSnapshot.CurrentUnitPrice, targetCurrency, calculatedSnapshot.Date),
				TotalInvested = await currencyExchange.ConvertMoney(calculatedSnapshot.TotalInvested, targetCurrency, calculatedSnapshot.Date),
				TotalValue = await currencyExchange.ConvertMoney(calculatedSnapshot.TotalValue, targetCurrency, calculatedSnapshot.Date),
				Quantity = calculatedSnapshot.Quantity,
			};
		}

		/// <summary>
		/// Basic holding information for optimized queries.
		/// </summary>
		private class HoldingBasicInfo
		{
			public long Id { get; set; }
			public AssetClass AssetClass { get; set; }
			public string? Name { get; set; }
			public string Symbol { get; set; } = string.Empty;
			public IList<SectorWeight> SectorWeights { get; set; } = new List<SectorWeight>();
		}

		/// <summary>
		/// Holding with its associated snapshots for optimized processing.
		/// </summary>
		private class HoldingWithSnapshots : HoldingBasicInfo
		{
			public List<CalculatedSnapshot> Snapshots { get; set; } = new();
		}
	}
}