using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class HoldingsDataService(DatabaseContext databaseContext) : IHoldingsDataService
	{
		public async Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default)
		{
			// TODO convert to target currency

			var holdings = await databaseContext.HoldingAggregateds.ToListAsync();

			return holdings
				.Select(h => {

					var lastSnapshot = h.CalculatedSnapshots.OrderByDescending(x => x.Date).FirstOrDefault(CalculatedSnapshot.Empty(targetCurrency));

					return new HoldingDisplayModel
					{
						AssetClass = h.AssetClass.ToString(),
						AveragePrice = lastSnapshot.AverageCostPrice.Amount,
						Currency = targetCurrency.Symbol.ToString(),
						CurrentValue = lastSnapshot.TotalValue.Amount,
						CurrentPrice = lastSnapshot.CurrentUnitPrice.Amount,
						GainLoss = lastSnapshot.TotalValue.Amount - lastSnapshot.TotalInvested.Amount,
						GainLossPercentage = lastSnapshot.TotalValue.Amount == 0 ? 0 : (lastSnapshot.TotalValue.Amount - lastSnapshot.TotalInvested.Amount) / lastSnapshot.TotalValue.Amount * 100,
						Name = h.Name ?? string.Empty,
						Quantity = lastSnapshot.Quantity,
						Symbol = h.Symbol,	
						Sector = h.SectorWeights?.ToString() ?? string.Empty,
						Weight  = 0,
					};
				})
				.ToList();
		}
	}
}