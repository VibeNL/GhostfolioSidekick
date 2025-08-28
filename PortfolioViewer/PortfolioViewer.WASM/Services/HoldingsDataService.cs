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
			// Step 1: Get all holdings with their basic information (simple query that SQLite can handle)
			var holdings = await databaseContext
				.HoldingAggregateds
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
				var latestDate = await databaseContext.CalculatedSnapshots
					.Where(s => EF.Property<long>(s, "HoldingAggregatedId") == holding.Id)
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
				var latestSnapshots = await databaseContext.CalculatedSnapshots
					.Where(s => EF.Property<long>(s, "HoldingAggregatedId") == holding.Id && s.Date == latestDate)
					.ToListAsync(cancellationToken);

				// Aggregate all snapshots for the latest date across all accounts
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
					var totalValueAtCurrentPrice = Money.Zero(firstCurrency);
					var totalValueAtAveragePrice = Money.Zero(firstCurrency);
					aggregatedTotalValue = Money.Zero(firstCurrency);
					aggregatedTotalInvested = Money.Zero(firstCurrency);
					
					foreach (var snapshot in latestSnapshots)
					{
						var valueAtCurrentPrice = snapshot.CurrentUnitPrice.Times(snapshot.Quantity);
						var valueAtAveragePrice = snapshot.AverageCostPrice.Times(snapshot.Quantity);
						
						totalValueAtCurrentPrice = totalValueAtCurrentPrice.Add(valueAtCurrentPrice);
						totalValueAtAveragePrice = totalValueAtAveragePrice.Add(valueAtAveragePrice);
						aggregatedTotalValue = aggregatedTotalValue.Add(snapshot.TotalValue);
						aggregatedTotalInvested = aggregatedTotalInvested.Add(snapshot.TotalInvested);
					}
					
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
					var totalValueAtCurrentPrice = Money.Zero(snapshotGroup.Snapshots.First().CurrentUnitPrice.Currency);
					var totalValueAtAveragePrice = Money.Zero(snapshotGroup.Snapshots.First().AverageCostPrice.Currency);
					
					foreach (var snapshot in snapshotGroup.Snapshots)
					{
						var valueAtCurrentPrice = snapshot.CurrentUnitPrice.Times(snapshot.Quantity);
						var valueAtAveragePrice = snapshot.AverageCostPrice.Times(snapshot.Quantity);
						
						totalValueAtCurrentPrice = totalValueAtCurrentPrice.Add(valueAtCurrentPrice);
						totalValueAtAveragePrice = totalValueAtAveragePrice.Add(valueAtAveragePrice);
					}
					
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