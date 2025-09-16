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
using System;
using System.Collections.Concurrent;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class HoldingsDataService(DatabaseContext databaseContext, ICurrencyExchange currencyExchange, ILogger<HoldingsDataService> logger) : IHoldingsDataService
	{
		// Currency conversion cache to reduce redundant conversions
		private readonly ConcurrentDictionary<string, Money> _currencyConversionCache = new();
		private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
		private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps = new();

		public async Task<List<HoldingDisplayModel>> GetHoldingsAsync(
			Currency targetCurrency,
			CancellationToken cancellationToken = default)
		{
			return await GetHoldingsAsync(targetCurrency, 0, cancellationToken);
		}

		public async Task<List<HoldingDisplayModel>> GetHoldingsAsync(
			Currency targetCurrency,
			int accountId,
			CancellationToken cancellationToken = default)
		{
			// **OPTIMIZATION 1: Single bulk query with joins instead of N+1 queries**
			var holdingsWithLatestSnapshots = await GetHoldingsWithLatestSnapshotsAsync(accountId, cancellationToken);
			
			if (!holdingsWithLatestSnapshots.Any())
			{
				return new List<HoldingDisplayModel>();
			}

			// **OPTIMIZATION 2: Batch currency conversions by grouping same currencies**
			var currencyGroups = holdingsWithLatestSnapshots
				.SelectMany(h => h.Snapshots)
				.GroupBy(s => s.TotalValue.Currency)
				.ToDictionary(g => g.Key, g => g.First().Date);

			// Pre-cache all needed currency conversions
			await PreCacheCurrencyConversionsAsync(currencyGroups.Keys, targetCurrency, currencyGroups.Values);

			var result = new List<HoldingDisplayModel>(holdingsWithLatestSnapshots.Count);

			// **OPTIMIZATION 3: Process holdings in parallel for CPU-bound operations**
			var tasks = holdingsWithLatestSnapshots.Select(async holding =>
			{
				if (!holding.Snapshots.Any())
				{
					// Create empty snapshot for holdings with no data
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
						Sector = holding.SectorWeights.Count != 0 ? string.Join(",", holding.SectorWeights.Select(x => x.Name)) : "Undefined",
						Weight = 0,
					};
				}

				// **OPTIMIZATION 4: Aggregate calculations optimized for minimal object allocation**
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
					Sector = holding.SectorWeights.Count != 0 ? string.Join(",", holding.SectorWeights.Select(x => x.Name)) : "Undefined",
					Weight = 0,
				};
			});

			var holdings = await Task.WhenAll(tasks);
			result.AddRange(holdings);

			// Calculate weights in a single pass
			var totalValue = result.Sum(x => x.CurrentValue.Amount);
			if (totalValue > 0)
			{
				foreach (var holding in result)
				{
					holding.Weight = holding.CurrentValue.Amount / totalValue;
				}
			}

			return result;
		}

		// **NEW: Optimized method to get holdings with their latest snapshots in a single query**
		private async Task<List<HoldingWithSnapshots>> GetHoldingsWithLatestSnapshotsAsync(int accountId, CancellationToken cancellationToken)
		{
			// Step 1: Get all holdings with basic info
			var holdingsQuery = databaseContext.HoldingAggregateds.AsNoTracking();

			var holdings = await holdingsQuery
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
			var latestDatesQuery = databaseContext.CalculatedSnapshots
				.Where(s => holdings.Select(h => h.Id).Contains(EF.Property<long>(s, "HoldingAggregatedId")))
				.AsNoTracking();

			if (accountId > 0)
			{
				latestDatesQuery = latestDatesQuery.Where(s => s.AccountId == accountId);
			}

			var latestDates = await latestDatesQuery
				.GroupBy(s => EF.Property<long>(s, "HoldingAggregatedId"))
				.Select(g => new { HoldingId = g.Key, LatestDate = g.Max(s => s.Date) })
				.ToDictionaryAsync(x => x.HoldingId, x => x.LatestDate, cancellationToken);

			// Step 3: Get all latest snapshots in one query
			var latestSnapshotsQuery = from snapshot in databaseContext.CalculatedSnapshots
									   where latestDates.Keys.Contains(EF.Property<long>(snapshot, "HoldingAggregatedId")) &&
											 latestDates.Values.Contains(snapshot.Date)
									   select new { 
										   Snapshot = snapshot, 
										   HoldingId = EF.Property<long>(snapshot, "HoldingAggregatedId") 
									   };

			if (accountId > 0)
			{
				latestSnapshotsQuery = latestSnapshotsQuery.Where(x => x.Snapshot.AccountId == accountId);
			}

			var allLatestSnapshots = await latestSnapshotsQuery
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			// Step 4: Group snapshots by holding and filter by latest date
			var snapshotsByHolding = allLatestSnapshots
				.Where(x => latestDates.TryGetValue(x.HoldingId, out var latestDate) && x.Snapshot.Date == latestDate)
				.GroupBy(x => x.HoldingId)
				.ToDictionary(g => g.Key, g => g.Select(x => x.Snapshot).ToList());

			// Step 5: Combine holdings with their snapshots
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

		// **NEW: Pre-cache currency conversions to reduce redundant API calls**
		private async Task PreCacheCurrencyConversionsAsync(
			IEnumerable<Currency> fromCurrencies, 
			Currency targetCurrency, 
			IEnumerable<DateOnly> dates)
		{
			var uniquePairs = fromCurrencies
				.Where(c => c != targetCurrency)
				.SelectMany(c => dates.Select(d => new { Currency = c, Date = d }))
				.Distinct()
				.ToList();

			var tasks = uniquePairs.Select(async pair =>
			{
				var cacheKey = GetCacheKey(pair.Currency, targetCurrency, pair.Date);
				if (!IsCacheValid(cacheKey))
				{
					try
					{
						var converted = await currencyExchange.ConvertMoney(
							new Money(pair.Currency, 1m), targetCurrency, pair.Date);
						_currencyConversionCache[cacheKey] = converted;
						_cacheTimestamps[cacheKey] = DateTime.UtcNow;
					}
					catch (Exception ex)
					{
						logger.LogWarning(ex, "Failed to cache currency conversion for {From} to {To} on {Date}", 
							pair.Currency.Symbol, targetCurrency.Symbol, pair.Date);
					}
				}
			});

			await Task.WhenAll(tasks);
		}

		// **NEW: Optimized snapshot aggregation with minimal allocations**
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
				var firstSnapshot = snapshots[0];
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

			// Use the first currency as base for aggregation
			var baseCurrency = snapshots[0].CurrentUnitPrice.Currency;
			
			decimal totalCurrentValue = 0;
			decimal totalAverageCostValue = 0;
			decimal totalInvestedValue = 0;
			decimal totalValueValue = 0;

			foreach (var snapshot in snapshots)
			{
				var currentValueAmount = snapshot.CurrentUnitPrice.Amount * snapshot.Quantity;
				var averageCostValueAmount = snapshot.AverageCostPrice.Amount * snapshot.Quantity;

				if (snapshot.CurrentUnitPrice.Currency == baseCurrency)
				{
					totalCurrentValue += currentValueAmount;
					totalAverageCostValue += averageCostValueAmount;
					totalInvestedValue += snapshot.TotalInvested.Amount;
					totalValueValue += snapshot.TotalValue.Amount;
				}
				else
				{
					// Use cached conversion
					var currentConversion = await ConvertMoneyOptimized(
						new Money(snapshot.CurrentUnitPrice.Currency, currentValueAmount), baseCurrency, latestDate);
					var averageConversion = await ConvertMoneyOptimized(
						new Money(snapshot.AverageCostPrice.Currency, averageCostValueAmount), baseCurrency, latestDate);
					var investedConversion = await ConvertMoneyOptimized(snapshot.TotalInvested, baseCurrency, latestDate);
					var valueConversion = await ConvertMoneyOptimized(snapshot.TotalValue, baseCurrency, latestDate);

					totalCurrentValue += currentConversion.Amount;
					totalAverageCostValue += averageConversion.Amount;
					totalInvestedValue += investedConversion.Amount;
					totalValueValue += valueConversion.Amount;
				}
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

		// **NEW: Optimized currency conversion with caching**
		private async Task<Money> ConvertMoneyOptimized(Money money, Currency targetCurrency, DateOnly date)
		{
			if (money.Currency == targetCurrency)
				return money;

			var cacheKey = GetCacheKey(money.Currency, targetCurrency, date);
			
			if (IsCacheValid(cacheKey) && _currencyConversionCache.TryGetValue(cacheKey, out var cachedRate))
			{
				return new Money(targetCurrency, money.Amount * cachedRate.Amount);
			}

			// Fall back to regular conversion
			var converted = await currencyExchange.ConvertMoney(money, targetCurrency, date);
			
			// Cache the rate for future use
			var rate = money.Amount == 0 ? Money.Zero(targetCurrency) : 
				new Money(targetCurrency, converted.Amount / money.Amount);
			_currencyConversionCache[cacheKey] = rate;
			_cacheTimestamps[cacheKey] = DateTime.UtcNow;

			return converted;
		}

		// **NEW: Optimized currency conversion for snapshots**
		private async Task<CalculatedSnapshot> ConvertToTargetCurrencyOptimized(Currency targetCurrency, CalculatedSnapshot calculatedSnapshot)
		{
			if (calculatedSnapshot.CurrentUnitPrice.Currency == targetCurrency)
			{
				return calculatedSnapshot;
			}

			return new CalculatedSnapshot
			{
				Date = calculatedSnapshot.Date,
				AverageCostPrice = await ConvertMoneyOptimized(calculatedSnapshot.AverageCostPrice, targetCurrency, calculatedSnapshot.Date),
				CurrentUnitPrice = await ConvertMoneyOptimized(calculatedSnapshot.CurrentUnitPrice, targetCurrency, calculatedSnapshot.Date),
				TotalInvested = await ConvertMoneyOptimized(calculatedSnapshot.TotalInvested, targetCurrency, calculatedSnapshot.Date),
				TotalValue = await ConvertMoneyOptimized(calculatedSnapshot.TotalValue, targetCurrency, calculatedSnapshot.Date),
				Quantity = calculatedSnapshot.Quantity,
			};
		}

		// **NEW: Cache management methods**
		private string GetCacheKey(Currency fromCurrency, Currency toCurrency, DateOnly date)
		{
			return $"{fromCurrency.Symbol}_{toCurrency.Symbol}_{date:yyyy-MM-dd}";
		}

		private bool IsCacheValid(string cacheKey)
		{
			return _cacheTimestamps.TryGetValue(cacheKey, out var timestamp) &&
				   DateTime.UtcNow - timestamp < _cacheExpiration;
		}

		public async Task<DateOnly> GetMinDateAsync(CancellationToken cancellationToken = default)
		{
			// Get the earliest date from the snapshots
			var minDate = await databaseContext.CalculatedSnapshots
				.OrderBy(s => s.Date)
				.Select(s => s.Date)
				.FirstOrDefaultAsync(cancellationToken);
			return minDate;
		}

		public async Task<List<PortfolioValueHistoryPoint>> GetPortfolioValueHistoryAsync(
			Currency targetCurrency,
			DateTime startDate,
			DateTime endDate,
			int accountId,
			CancellationToken cancellationToken = default)
		{
			// Query snapshots in date range and filter by account
			var query = databaseContext.CalculatedSnapshots
				.Where(s => s.Date >= DateOnly.FromDateTime(startDate) && s.Date <= DateOnly.FromDateTime(endDate));

			if (accountId > 0)
			{
				query = query.Where(s => s.AccountId == accountId);
			}

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

		public async Task<List<Account>> GetAccountsAsync()
		{
			try
			{
				logger.LogInformation("Loading accounts from database...");
				
				// Ensure database is available and connection is working
				if (!await databaseContext.Database.CanConnectAsync())
				{
					logger.LogError("Cannot connect to database when loading accounts");
					throw new InvalidOperationException("Database connection failed. Please check your database configuration.");
				}
				
				var accounts = await databaseContext.Accounts
					.AsNoTracking() // Optimize for read-only operations
					.OrderBy(a => a.Name)
					.ToListAsync();
				
				logger.LogInformation("Successfully loaded {Count} accounts", accounts.Count);
				return accounts;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to load accounts from database");
				throw new InvalidOperationException("Failed to load accounts. Please try again later.", ex);
			}
		}

		public async Task<List<string>> GetSymbolsAsync()
		{
			try
			{
				logger.LogInformation("Loading symbols from database...");
				
				// Ensure database is available and connection is working
				if (!await databaseContext.Database.CanConnectAsync())
				{
					logger.LogError("Cannot connect to database when loading symbols");
					throw new InvalidOperationException("Database connection failed. Please check your database configuration.");
				}
				
				var symbols = await databaseContext.SymbolProfiles
					.AsNoTracking() // Optimize for read-only operations
					.Select(sp => sp.Symbol)
					.Distinct()
					.OrderBy(s => s)
					.ToListAsync();
				
				logger.LogInformation("Successfully loaded {Count} unique symbols", symbols.Count);
				return symbols;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to load symbols from database");
				throw new InvalidOperationException("Failed to load symbols. Please try again later.", ex);
			}
		}

		public async Task<List<string>> GetSymbolsByAccountAsync(int accountId)
		{
			try
			{
				logger.LogInformation("Loading symbols for account {AccountId} from database...", accountId);
				
				// Ensure database is available and connection is working
				if (!await databaseContext.Database.CanConnectAsync())
				{
					logger.LogError("Cannot connect to database when loading symbols by account");
					throw new InvalidOperationException("Database connection failed. Please check your database configuration.");
				}
				
				var symbols = await databaseContext.Activities
					.Where(a => a.Account.Id == accountId && a.Holding != null)
					.SelectMany(a => a.Holding!.SymbolProfiles)
					.Select(sp => sp.Symbol)
					.Distinct()
					.OrderBy(s => s)
					.AsNoTracking()
					.ToListAsync();
				
				logger.LogInformation("Successfully loaded {Count} unique symbols for account {AccountId}", symbols.Count, accountId);
				return symbols;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to load symbols for account {AccountId} from database", accountId);
				throw new InvalidOperationException($"Failed to load symbols for account {accountId}. Please try again later.", ex);
			}
		}

		public async Task<List<Account>> GetAccountsBySymbolAsync(string symbol)
		{
			try
			{
				logger.LogInformation("Loading accounts for symbol {Symbol} from database...", symbol);
				
				// Ensure database is available and connection is working
				if (!await databaseContext.Database.CanConnectAsync())
				{
					logger.LogError("Cannot connect to database when loading accounts by symbol");
					throw new InvalidOperationException("Database connection failed. Please check your database configuration.");
				}
				
				var accounts = await databaseContext.Activities
					.Where(a => a.Holding != null && a.Holding.SymbolProfiles.Any(sp => sp.Symbol == symbol))
					.Select(a => a.Account)
					.Distinct()
					.OrderBy(a => a.Name)
					.AsNoTracking()
					.ToListAsync();
				
				logger.LogInformation("Successfully loaded {Count} accounts for symbol {Symbol}", accounts.Count, symbol);
				return accounts;
			}
			catch (Exception ex)
			{
			logger.LogError(ex, "Failed to load accounts for symbol {Symbol} from database", symbol);
				throw new InvalidOperationException($"Failed to load accounts for symbol {symbol}. Please try again later.", ex);
			}
		}

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

				var startDateOnly = DateOnly.FromDateTime(startDate);
				var endDateOnly = DateOnly.FromDateTime(endDate);

				// Get all accounts first
				var accounts = await databaseContext.Accounts
					.AsNoTracking()
					.OrderBy(a => a.Name)
					.ToListAsync(cancellationToken);

				if (!accounts.Any())
				{
					logger.LogInformation("No accounts found");
					return new List<AccountValueHistoryPoint>();
				}

				// Optimization 1: Bulk query for all snapshots and balances
				var allSnapshots = await databaseContext.CalculatedSnapshots
					.Where(s => s.Date >= startDateOnly && s.Date <= endDateOnly)
					.OrderBy(s => s.Date)
					.AsNoTracking()
					.ToListAsync(cancellationToken);

				var allBalances = await databaseContext.Set<Balance>()
					.Where(b => b.Date >= startDateOnly && b.Date <= endDateOnly)
					.OrderBy(b => b.Date)
					.AsNoTracking()
					.ToListAsync(cancellationToken);

				// Group data by account for processing
				var snapshotsByAccount = allSnapshots.GroupBy(s => s.AccountId).ToDictionary(g => g.Key, g => g.ToList());
				var balancesByAccount = allBalances.GroupBy(b => b.AccountId).ToDictionary(g => g.Key, g => g.ToList());

				// Filter accounts that have data
				var accountsWithData = accounts.Where(a => 
					snapshotsByAccount.ContainsKey(a.Id) || balancesByAccount.ContainsKey(a.Id)).ToList();

				if (!accountsWithData.Any())
				{
					logger.LogInformation("No account data found for the specified date range");
					return new List<AccountValueHistoryPoint>();
				}

				// Optimization 2: Currency conversion caching
				var currencyConversionCache = new Dictionary<string, Money>();
				
				var result = new List<AccountValueHistoryPoint>();

				// Process each account
				foreach (var account in accountsWithData)
				{
					var accountSnapshots = snapshotsByAccount.GetValueOrDefault(account.Id, new List<CalculatedSnapshot>());
					var accountBalances = balancesByAccount.GetValueOrDefault(account.Id, new List<Balance>());

					// Group snapshots by date for faster lookups
					var snapshotsByDate = accountSnapshots.GroupBy(s => s.Date).ToDictionary(g => g.Key, g => g.ToList());
					var balancesByDate = accountBalances.ToDictionary(b => b.Date, b => b);

					// Get all dates that have data for this account
					var accountDates = accountSnapshots.Select(s => s.Date)
						.Union(accountBalances.Select(b => b.Date))
						.Distinct()
						.OrderBy(d => d);

					foreach (var date in accountDates)
					{
						decimal totalValueAmount = 0;
						decimal totalInvestedAmount = 0;
						decimal accountBalanceAmount = 0;

						// Process snapshots for this date
						if (snapshotsByDate.TryGetValue(date, out var dateSnapshots))
						{
							// Group by currency to minimize conversions
							var snapshotsByCurrency = dateSnapshots.GroupBy(s => s.TotalValue.Currency).ToList();

							foreach (var currencyGroup in snapshotsByCurrency)
							{
								var currency = currencyGroup.Key;
								var totalValueInCurrency = currencyGroup.Sum(s => s.TotalValue.Amount);
								var totalInvestedInCurrency = currencyGroup.Sum(s => s.TotalInvested.Amount);

								// Use cache for currency conversion
								var cacheKey = $"{currency.Symbol}_{date:yyyy-MM-dd}";
								if (!currencyConversionCache.TryGetValue(cacheKey, out var conversionRate))
								{
									conversionRate = await currencyExchange.ConvertMoney(
										new Money(currency, 1), targetCurrency, date);
									currencyConversionCache[cacheKey] = conversionRate;
								}

								totalValueAmount += totalValueInCurrency * conversionRate.Amount;
								totalInvestedAmount += totalInvestedInCurrency * conversionRate.Amount;
							}
						}

						// Process balance for this date (get most recent balance up to this date)
						var relevantBalance = accountBalances
							.Where(b => b.Date <= date)
							.OrderByDescending(b => b.Date)
							.FirstOrDefault();

						if (relevantBalance != null)
						{
							var cacheKey = $"{relevantBalance.Money.Currency.Symbol}_{date:yyyy-MM-dd}";
							if (!currencyConversionCache.TryGetValue(cacheKey, out var conversionRate))
							{
								conversionRate = await currencyExchange.ConvertMoney(
									new Money(relevantBalance.Money.Currency, 1), targetCurrency, date);
								currencyConversionCache[cacheKey] = conversionRate;
							}

							accountBalanceAmount = relevantBalance.Money.Amount * conversionRate.Amount;
						}

						// Create the result point
						result.Add(new AccountValueHistoryPoint
						{
							Date = date,
							Account = account,
							Value = new Money(targetCurrency, totalValueAmount + accountBalanceAmount),
							Invested = new Money(targetCurrency, totalInvestedAmount),
							Balance = new Money(targetCurrency, accountBalanceAmount)
						});
					}
				}

				logger.LogInformation("Successfully loaded account value history for {AccountCount} accounts across {PointCount} data points using optimized query", 
					accountsWithData.Count, result.Count);
				
				return result.OrderBy(r => r.Date).ThenBy(r => r.Account.Name).ToList();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Failed to load account value history from database");
				throw new InvalidOperationException("Failed to load account value history. Please try again later.", ex);
			}
		}

		public async Task<List<HoldingPriceHistoryPoint>> GetHoldingPriceHistoryAsync(
			string symbol,
			DateTime startDate,
			DateTime endDate,
			CancellationToken cancellationToken = default)
		{
			// Get price history from the holding's calculated snapshots
			var snapshots = await databaseContext.HoldingAggregateds
				.Where(h => h.Symbol == symbol)
				.SelectMany(h => h.CalculatedSnapshots)
				.Where(s => s.Date >= DateOnly.FromDateTime(startDate) &&
						   s.Date <= DateOnly.FromDateTime(endDate))
				.GroupBy(s => s.Date)
				.OrderBy(g => g.Key)
				.Select(g => new
				{
					Date = g.Key,
					Snapshots = g.ToList()
				})
				.ToListAsync(cancellationToken);

			var priceHistory = new List<HoldingPriceHistoryPoint>();
			
			foreach (var snapshotGroup in snapshots)
			{
				// Calculate quantity-weighted average for current unit price and average cost price
				var totalQuantity = snapshotGroup.Snapshots.Sum(s => s.Quantity);
				
				Money aggregatedCurrentPrice;
				Money aggregatedAveragePrice;
				
				if (totalQuantity == 0)
				{
					// If total quantity is zero, use the first snapshot's prices
					var firstSnapshot = snapshotGroup.Snapshots.First();
					aggregatedCurrentPrice = firstSnapshot.CurrentUnitPrice;
					aggregatedAveragePrice = firstSnapshot.AverageCostPrice;
				}
				else
				{
					// Calculate quantity-weighted average prices
					var firstCurrency = snapshotGroup.Snapshots.First().CurrentUnitPrice.Currency;
					
					decimal totalValueAtCurrentPriceAmount = 0;
					decimal totalValueAtAveragePriceAmount = 0;
					
					foreach (var snapshot in snapshotGroup.Snapshots)
					{
						var valueAtCurrentPrice = snapshot.CurrentUnitPrice.Times(snapshot.Quantity);
						var valueAtAveragePrice = snapshot.AverageCostPrice.Times(snapshot.Quantity);
						
						// Convert to first currency if needed and sum amounts
						if (valueAtCurrentPrice.Currency == firstCurrency)
						{
							totalValueAtCurrentPriceAmount += valueAtCurrentPrice.Amount;
						}
						else
						{
							var converted = await currencyExchange.ConvertMoney(valueAtCurrentPrice, firstCurrency, snapshotGroup.Date);
							totalValueAtCurrentPriceAmount += converted.Amount;
						}
						
						if (valueAtAveragePrice.Currency == firstCurrency)
						{
							totalValueAtAveragePriceAmount += valueAtAveragePrice.Amount;
						}
						else
						{
							var converted = await currencyExchange.ConvertMoney(valueAtAveragePrice, firstCurrency, snapshotGroup.Date);
							totalValueAtAveragePriceAmount += converted.Amount;
						}
					}
					
					var totalValueAtCurrentPrice = new Money(firstCurrency, totalValueAtCurrentPriceAmount);
					var totalValueAtAveragePrice = new Money(firstCurrency, totalValueAtAveragePriceAmount);
					
					aggregatedCurrentPrice = totalValueAtCurrentPrice.SafeDivide(totalQuantity);
					aggregatedAveragePrice = totalValueAtAveragePrice.SafeDivide(totalQuantity);
				}

				priceHistory.Add(new HoldingPriceHistoryPoint
				{
					Date = snapshotGroup.Date,
					Price = aggregatedCurrentPrice,
					AveragePrice = aggregatedAveragePrice
				});
			}

			return priceHistory;
		}

		public async Task<List<TransactionDisplayModel>> GetTransactionsAsync(
			Currency targetCurrency,
			DateTime startDate,
			DateTime endDate,
			int accountId,
			string symbol,
			CancellationToken cancellationToken = default)
		{
			// Build the base query with simple includes first
			var baseQuery = databaseContext.Activities
				.Include(a => a.Account)
				.Include(a => a.Holding)
				.Where(a => a.Date >= startDate && a.Date <= endDate);

			// Apply account filter
			if (accountId > 0)
			{
				baseQuery = baseQuery.Where(a => a.Account.Id == accountId);
			}

			// Execute the base query first without symbol filtering
			var activities = await baseQuery
				.OrderByDescending(a => a.Date)
				.ThenBy(a => a.Id)
				.ToListAsync(cancellationToken);

			// Apply symbol filter in memory after loading the basic data
			if (!string.IsNullOrEmpty(symbol))
			{
				// Load symbol profiles separately for activities with holdings
				var holdingIds = activities
					.Where(a => a.Holding != null)
					.Select(a => a.Holding!.Id)
					.Distinct()
					.ToList();

				if (holdingIds.Count > 0)
				{
					var holdingsWithSymbols = await databaseContext.Holdings
						.Where(h => holdingIds.Contains(h.Id))
						.Include(h => h.SymbolProfiles)
						.ToDictionaryAsync(h => h.Id, h => h.SymbolProfiles, cancellationToken);

					// Filter activities by symbol in memory
					activities = activities
						.Where(a => a.Holding != null &&
								   holdingsWithSymbols.TryGetValue(a.Holding.Id, out var symbolProfiles) && 
								    symbolProfiles.Any(sp => sp.Symbol == symbol))
						.ToList();

					// Populate the SymbolProfiles navigation property for filtered activities
					foreach (var activity in activities.Where(a => a.Holding != null))
					{
						if (holdingsWithSymbols.TryGetValue(activity.Holding!.Id, out var symbolProfiles))
						{
							activity.Holding.SymbolProfiles = symbolProfiles.ToList();
						}
					}
				}
				else
				{
					// No holdings found, filter out all activities that require holdings
					activities = activities.Where(a => a.Holding == null).ToList();
				}
			}
			else
			{
				// Load symbol profiles for all holdings if no symbol filter is applied
				var holdingIds = activities
					.Where(a => a.Holding != null)
					.Select(a => a.Holding!.Id)
					.Distinct()
					.ToList();

				if (holdingIds.Count > 0)
				{
					var holdingsWithSymbols = await databaseContext.Holdings
						.Where(h => holdingIds.Contains(h.Id))
						.Include(h => h.SymbolProfiles)
						.ToDictionaryAsync(h => h.Id, h => h.SymbolProfiles, cancellationToken);

					// Populate the SymbolProfiles navigation property
					foreach (var activity in activities.Where(a => a.Holding != null))
					{
						if (holdingsWithSymbols.TryGetValue(activity.Holding!.Id, out var symbolProfiles))
						{
							activity.Holding.SymbolProfiles = symbolProfiles.ToList();
						}
					}
				}
			}

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

			// Map specific activity types
			switch (activity)
			{
				case BuyActivity buySell:
					baseModel.Quantity = buySell.Quantity;
					baseModel.UnitPrice = await ConvertMoney(buySell.UnitPrice, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Amount = await ConvertMoney(buySell.UnitPrice.Times(buySell.Quantity), targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = await ConvertMoney(buySell.TotalTransactionAmount, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Fee = await SumAndConvertFees(buySell.Fees.Select(f => f.Money), targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Tax = await SumAndConvertFees(buySell.Taxes.Select(t => t.Money), targetCurrency, DateOnly.FromDateTime(activity.Date));
					break;
				case SellActivity buySell:
					baseModel.Quantity = buySell.Quantity;
					baseModel.UnitPrice = await ConvertMoney(buySell.UnitPrice, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Amount = await ConvertMoney(buySell.UnitPrice.Times(buySell.Quantity), targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = await ConvertMoney(buySell.TotalTransactionAmount, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Fee = await SumAndConvertFees(buySell.Fees.Select(f => f.Money), targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Tax = await SumAndConvertFees(buySell.Taxes.Select(t => t.Money), targetCurrency, DateOnly.FromDateTime(activity.Date));
					break;
				case DividendActivity dividend:
					baseModel.Amount = await ConvertMoney(dividend.Amount, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = baseModel.Amount;
					baseModel.Fee = await SumAndConvertFees(dividend.Fees.Select(f => f.Money), targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Tax = await SumAndConvertFees(dividend.Taxes.Select(t => t.Money), targetCurrency, DateOnly.FromDateTime(activity.Date));
					break;
				case CashDepositActivity deposit:
					baseModel.Amount = await ConvertMoney(deposit.Amount, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = baseModel.Amount;
					break;
				case CashWithdrawalActivity withdrawal:
					baseModel.Amount = await ConvertMoney(withdrawal.Amount, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = baseModel.Amount;
					break;
				case FeeActivity fee:
					baseModel.Amount = await ConvertMoney(fee.Amount, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = baseModel.Amount;
					baseModel.Fee = baseModel.Amount;
					break;
				case InterestActivity interest:
					baseModel.Amount = await ConvertMoney(interest.Amount, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = baseModel.Amount;
					break;
				case ReceiveActivity sendReceive:
					baseModel.Quantity = sendReceive.Quantity;
					baseModel.UnitPrice = await ConvertMoney(sendReceive.UnitPrice, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Amount = await ConvertMoney(sendReceive.UnitPrice.Times(sendReceive.Quantity), targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = baseModel.Amount;
					baseModel.Fee = await SumAndConvertFees(sendReceive.Fees.Select(f => f.Money), targetCurrency, DateOnly.FromDateTime(activity.Date));
					break;
				case SendActivity sendReceive:
					baseModel.Quantity = sendReceive.Quantity;
					baseModel.UnitPrice = await ConvertMoney(sendReceive.UnitPrice, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Amount = await ConvertMoney(sendReceive.UnitPrice.Times(sendReceive.Quantity), targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = baseModel.Amount;
					baseModel.Fee = await SumAndConvertFees(sendReceive.Fees.Select(f => f.Money), targetCurrency, DateOnly.FromDateTime(activity.Date));
					break;
				case StakingRewardActivity staking:
					baseModel.Quantity = staking.Quantity;
					baseModel.UnitPrice = await ConvertMoney(staking.UnitPrice, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Amount = await ConvertMoney(staking.UnitPrice.Times(staking.Quantity), targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = baseModel.Amount;
					break;
				case GiftAssetActivity gift:
					baseModel.Quantity = gift.Quantity;
					baseModel.UnitPrice = await ConvertMoney(gift.UnitPrice, targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.Amount = await ConvertMoney(gift.UnitPrice.Times(gift.Quantity), targetCurrency, DateOnly.FromDateTime(activity.Date));
					baseModel.TotalValue = baseModel.Amount;
					break;
				default:
					// For other activity types, just return the basic info
					break;
			}

			return baseModel;
		}

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

		private async Task<Money?> SumAndConvertFees(IEnumerable<Money> fees, Currency targetCurrency, DateOnly date)
		{
			var feeList = fees.ToList();
			if (feeList.Count == 0)
				return null;

			var convertedFees = new List<Money>();
			foreach (var fee in feeList)
			{
				convertedFees.Add(await ConvertMoney(fee, targetCurrency, date));
			}

			return Money.Sum(convertedFees);
		}

		private async Task<Money> ConvertMoney(Money money, Currency targetCurrency, DateOnly date)
		{
			if (money.Currency == targetCurrency)
				return money;

			return await currencyExchange.ConvertMoney(money, targetCurrency, date);
		}

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

		// **NEW: Helper classes for optimized data structures**
		private class HoldingBasicInfo
		{
			public long Id { get; set; }
			public AssetClass AssetClass { get; set; }
			public string? Name { get; set; }
			public string Symbol { get; set; } = string.Empty;
			public IList<SectorWeight> SectorWeights { get; set; } = new List<SectorWeight>();
		}

		private class HoldingWithSnapshots : HoldingBasicInfo
		{
			public List<CalculatedSnapshot> Snapshots { get; set; } = new();
		}
	}
}