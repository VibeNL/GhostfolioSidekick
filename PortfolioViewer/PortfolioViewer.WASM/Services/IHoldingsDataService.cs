using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.PerformanceCalculations;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    /// <summary>
    /// Interface for portfolio data services. Implement this interface to provide real data to the Holdings page.
    /// </summary>
    public interface IHoldingsDataService
    {
        /// <summary>
        /// Loads all holdings for the current portfolio
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the async operation</param>
        /// <returns>List of holdings with their current values and performance data</returns>
        Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default);
    }

	public class HoldingsDataService(IHoldingPerformanceCalculator holdingPerformanceCalculator) : IHoldingsDataService
	{
		public async Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default)
		{
			var holdings = await holdingPerformanceCalculator.GetCalculatedHoldings(targetCurrency);

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