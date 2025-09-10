using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class HoldingsDataService(DatabaseContext databaseContext, ICurrencyExchange currencyExchange, ILogger<HoldingsDataService> logger) : IHoldingsDataService
	{
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
			// Step 1: Get all holdings with their basic information (simple query that SQLite can handle)
			var holdingsQuery = databaseContext
				.HoldingAggregateds
				.AsQueryable();

			// If accountId is specified, filter holdings to only those that have snapshots for that account
			if (accountId > 0)
			{
				holdingsQuery = holdingsQuery
					.Where(h => h.CalculatedSnapshots.Any(s => s.AccountId == accountId));
			}

			var holdings = await holdingsQuery
				.Select(h => new
				{
					Id = h.Id,
					AssetClass = h.AssetClass,
					Name = h.Name,
					Symbol = h.Symbol,
					SectorWeights = h.SectorWeights
				})
				.ToListAsync(cancellationToken);

			var list = new List<HoldingDisplayModel>();

			foreach (var holding in holdings)
			{
				// Step 2: Get the latest date for this specific holding using EF.Property for shadow property
				var latestDateQuery = databaseContext.CalculatedSnapshots
					.Where(s => EF.Property<long>(s, "HoldingAggregatedId") == holding.Id);

				// Filter by account if specified
				if (accountId > 0)
				{
					latestDateQuery = latestDateQuery.Where(s => s.AccountId == accountId);
				}

				var latestDate = await latestDateQuery
					.OrderByDescending(s => s.Date)
					.Select(s => s.Date)
					.FirstOrDefaultAsync(cancellationToken);

				if (latestDate == default)
				{
					// Create empty snapshot for holdings with no data
					var emptySnapshot = CalculatedSnapshot.Empty(targetCurrency, 0);
					var convertedEmptySnapshot = await ConvertToTargetCurrency(targetCurrency, emptySnapshot);
					list.Add(new HoldingDisplayModel
					{
						AssetClass = holding.AssetClass.ToString(),
						AveragePrice = convertedEmptySnapshot.AverageCostPrice,
						Currency = targetCurrency.Symbol.ToString(),
						CurrentValue = convertedEmptySnapshot.TotalValue,
						CurrentPrice = convertedEmptySnapshot.CurrentUnitPrice,
						GainLoss = convertedEmptySnapshot.TotalValue.Subtract(convertedEmptySnapshot.TotalInvested),
						GainLossPercentage = 0,
						Name = holding.Name ?? string.Empty,
						Quantity = convertedEmptySnapshot.Quantity,
						Symbol = holding.Symbol,
						Sector = holding.SectorWeights.Count != 0 ? string.Join(",", holding.SectorWeights.Select(x => x.Name)) : "Undefined",
						Weight = 0,
					});
					continue;
				}

				// Step 3: Get all snapshots for the latest date for this holding
				var latestSnapshotsQuery = databaseContext.CalculatedSnapshots
					.Where(s => EF.Property<long>(s, "HoldingAggregatedId") == holding.Id && s.Date == latestDate);

				// Filter by account if specified
				if (accountId > 0)
				{
					latestSnapshotsQuery = latestSnapshotsQuery.Where(s => s.AccountId == accountId);
				}

				var latestSnapshots = await latestSnapshotsQuery.ToListAsync(cancellationToken);

				// Aggregate all snapshots for the latest date across filtered accounts
				var totalQuantity = latestSnapshots.Sum(s => s.Quantity);
				
				Money aggregatedCurrentPrice;
				Money aggregatedAveragePrice;
				Money aggregatedTotalValue;
				Money aggregatedTotalInvested;
				
				if (totalQuantity == 0)
				{
					// If total quantity is zero, use the first snapshot's prices
					var firstSnapshot = latestSnapshots.First();
					aggregatedCurrentPrice = firstSnapshot.CurrentUnitPrice;
					aggregatedAveragePrice = firstSnapshot.AverageCostPrice;
					aggregatedTotalValue = Money.Zero(firstSnapshot.TotalValue.Currency);
					aggregatedTotalInvested = Money.Zero(firstSnapshot.TotalInvested.Currency);
				}
				else
				{
					// Calculate quantity-weighted average prices and sum total values
					var firstCurrency = latestSnapshots.First().CurrentUnitPrice.Currency;
					
					decimal totalValueAtCurrentPriceAmount = 0;
					decimal totalValueAtAveragePriceAmount = 0;
					decimal aggregatedTotalValueAmount = 0;
					decimal aggregatedTotalInvestedAmount = 0;
					
					foreach (var snapshot in latestSnapshots)
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
							var converted = await currencyExchange.ConvertMoney(valueAtCurrentPrice, firstCurrency, latestDate);
							totalValueAtCurrentPriceAmount += converted.Amount;
						}
						
						if (valueAtAveragePrice.Currency == firstCurrency)
						{
							totalValueAtAveragePriceAmount += valueAtAveragePrice.Amount;
						}
						else
						{
							var converted = await currencyExchange.ConvertMoney(valueAtAveragePrice, firstCurrency, latestDate);
							totalValueAtAveragePriceAmount += converted.Amount;
						}
						
						if (snapshot.TotalValue.Currency == firstCurrency)
						{
							aggregatedTotalValueAmount += snapshot.TotalValue.Amount;
						}
						else
						{
							var converted = await currencyExchange.ConvertMoney(snapshot.TotalValue, firstCurrency, latestDate);
							aggregatedTotalValueAmount += converted.Amount;
						}
						
						if (snapshot.TotalInvested.Currency == firstCurrency)
						{
							aggregatedTotalInvestedAmount += snapshot.TotalInvested.Amount;
						}
						else
						{
							var converted = await currencyExchange.ConvertMoney(snapshot.TotalInvested, firstCurrency, latestDate);
							aggregatedTotalInvestedAmount += converted.Amount;
						}
					}
					
					var totalValueAtCurrentPrice = new Money(firstCurrency, totalValueAtCurrentPriceAmount);
					var totalValueAtAveragePrice = new Money(firstCurrency, totalValueAtAveragePriceAmount);
					aggregatedTotalValue = new Money(firstCurrency, aggregatedTotalValueAmount);
					aggregatedTotalInvested = new Money(firstCurrency, aggregatedTotalInvestedAmount);
					
					aggregatedCurrentPrice = totalValueAtCurrentPrice.SafeDivide(totalQuantity);
					aggregatedAveragePrice = totalValueAtAveragePrice.SafeDivide(totalQuantity);
				}

				// Create aggregated snapshot for currency conversion
				var aggregatedSnapshot = new CalculatedSnapshot
				{
					Date = latestDate,
					AverageCostPrice = aggregatedAveragePrice,
					CurrentUnitPrice = aggregatedCurrentPrice,
					TotalInvested = aggregatedTotalInvested,
					TotalValue = aggregatedTotalValue,
					Quantity = totalQuantity,
				};

				var convertedSnapshot = await ConvertToTargetCurrency(targetCurrency, aggregatedSnapshot);
				list.Add(new HoldingDisplayModel
				{
					AssetClass = holding.AssetClass.ToString(),
					AveragePrice = convertedSnapshot.AverageCostPrice,
					Currency = targetCurrency.Symbol.ToString(),
					CurrentValue = convertedSnapshot.TotalValue,
					CurrentPrice = convertedSnapshot.CurrentUnitPrice,
					GainLoss = convertedSnapshot.TotalValue.Subtract(convertedSnapshot.TotalInvested),
					GainLossPercentage = convertedSnapshot.TotalValue.Amount == 0 ? 0 : (convertedSnapshot.TotalValue.Amount - convertedSnapshot.TotalInvested.Amount) / convertedSnapshot.TotalValue.Amount,
					Name = holding.Name ?? string.Empty,
					Quantity = convertedSnapshot.Quantity,
					Symbol = holding.Symbol,
					Sector = holding.SectorWeights.Count != 0 ? string.Join(",", holding.SectorWeights.Select(x => x.Name)) : "Undefined",
					Weight = 0,
				});
			}

			// Calculate weights
			var totalValue = list.Sum(x => x.CurrentValue.Amount);
			if (totalValue > 0)
			{
				foreach (var holding in list)
				{
					holding.Weight = holding.CurrentValue.Amount / totalValue;
				}
			}

			return list;
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
							       (holdingsWithSymbols.TryGetValue(a.Holding.Id, out var symbolProfiles) && 
							        symbolProfiles.Any(sp => sp.Symbol == symbol)))
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
	}
}