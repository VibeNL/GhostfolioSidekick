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
			// Step 1: Project only the required fields and the ID of the latest snapshot
			var holdingProjections = await databaseContext
				.HoldingAggregateds
				.Select(h => new
				{
					AssetClass = h.AssetClass,
					Name = h.Name,
					Symbol = h.Symbol,
					SectorWeights = h.SectorWeights,
					LastSnapshotId = h.CalculatedSnapshots
						.OrderByDescending(x => x.Date)
						.Select(x => (long?)x.Id)
						.FirstOrDefault()
				})
				.ToListAsync(cancellationToken);

			// Step 2: Fetch all required snapshots in one go
			var snapshotIds = holdingProjections
				.Where(h => h.LastSnapshotId.HasValue)
				.Select(h => h.LastSnapshotId.Value)
				.ToList();

			var snapshotsDict = await databaseContext.CalculatedSnapshots
				.Where(x => snapshotIds.Contains(x.Id))
				.ToDictionaryAsync(x => x.Id, cancellationToken);

			var list = new List<HoldingDisplayModel>();

			foreach (var h in holdingProjections)
			{
				CalculatedSnapshot lastSnapshot;
				if (h.LastSnapshotId.HasValue && snapshotsDict.TryGetValue(h.LastSnapshotId.Value, out var snap))
				{
					lastSnapshot = snap;
				}
				else
				{
					lastSnapshot = CalculatedSnapshot.Empty(targetCurrency, 0);
				}

				var convertedLastSnapshot = await ConvertToTargetCurrency(targetCurrency, lastSnapshot);
				list.Add(new HoldingDisplayModel
				{
					AssetClass = h.AssetClass.ToString(),
					AveragePrice = convertedLastSnapshot.AverageCostPrice,
					Currency = targetCurrency.Symbol.ToString(),
					CurrentValue = convertedLastSnapshot.TotalValue,
					CurrentPrice = convertedLastSnapshot.CurrentUnitPrice,
					GainLoss = convertedLastSnapshot.TotalValue.Subtract(convertedLastSnapshot.TotalInvested),
					GainLossPercentage = convertedLastSnapshot.TotalValue.Amount == 0 ? 0 : (convertedLastSnapshot.TotalValue.Amount - convertedLastSnapshot.TotalInvested.Amount) / convertedLastSnapshot.TotalValue.Amount,
					Name = h.Name ?? string.Empty,
					Quantity = convertedLastSnapshot.Quantity,
					Symbol = h.Symbol,
					Sector = h.SectorWeights.Count != 0 ? string.Join(",", h.SectorWeights.Select(x => x.Name)) : "Undefined",
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

		public async Task<List<PortfolioValueHistoryPoint>> GetHoldingPriceHistoryAsync(
			string symbol,
			DateTime startDate,
			DateTime endDate,
			CancellationToken cancellationToken = default)
		{
			// Get the holding aggregated first, then get its snapshots
			var holdingAggregated = await databaseContext.HoldingAggregateds
				.Include(h => h.CalculatedSnapshots)
				.FirstOrDefaultAsync(h => h.Symbol == symbol, cancellationToken);

			if (holdingAggregated == null)
			{
				return new List<PortfolioValueHistoryPoint>();
			}

			var snapshots = holdingAggregated.CalculatedSnapshots
				.Where(s => s.Date >= DateOnly.FromDateTime(startDate) && s.Date <= DateOnly.FromDateTime(endDate))
				.OrderBy(s => s.Date)
				.Select(s => new PortfolioValueHistoryPoint
				{
					Date = s.Date,
					Value = new Money[] { s.TotalValue },
					Invested = new Money[] { s.TotalInvested }
				})
				.ToList();

			return snapshots;
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