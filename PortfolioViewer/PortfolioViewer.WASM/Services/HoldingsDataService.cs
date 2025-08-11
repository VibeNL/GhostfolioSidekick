using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.EntityFrameworkCore;
using System;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class HoldingsDataService(DatabaseContext databaseContext, ICurrencyExchange currencyExchange) : IHoldingsDataService
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
			return await databaseContext.Accounts.ToListAsync();
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