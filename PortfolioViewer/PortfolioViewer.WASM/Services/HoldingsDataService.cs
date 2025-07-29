using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;

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
				CalculatedSnapshot lastSnapshot = null;
				if (h.LastSnapshotId.HasValue && snapshotsDict.TryGetValue(h.LastSnapshotId.Value, out var snap))
				{
					lastSnapshot = snap;
				}
				else
				{
					lastSnapshot = CalculatedSnapshot.Empty(targetCurrency);
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
					Sector = h.SectorWeights.Any() ? string.Join(",", h.SectorWeights.Select(x => x.Name)) : "Undefined",
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