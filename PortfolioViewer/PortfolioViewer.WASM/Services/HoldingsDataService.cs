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
			var holdings = await databaseContext
				.HoldingAggregateds
				.Select(x => new
				{
					Holding = x,
					LastSnapshot = x.CalculatedSnapshots.OrderByDescending(x => x.Date).FirstOrDefault(CalculatedSnapshot.Empty(targetCurrency))
				})
				.ToListAsync();

			var list = new List<HoldingDisplayModel>();

			foreach (var h in holdings)
			{
				var convertedLastSnapshot = await ConvertToTargetCurrency(targetCurrency, h.LastSnapshot);
				list.Add(new HoldingDisplayModel
				{
					AssetClass = h.Holding.AssetClass.ToString(),
					AveragePrice = convertedLastSnapshot.AverageCostPrice.Amount,
					Currency = targetCurrency.Symbol.ToString(),
					CurrentValue = convertedLastSnapshot.TotalValue.Amount,
					CurrentPrice = convertedLastSnapshot.CurrentUnitPrice.Amount,
					GainLoss = convertedLastSnapshot.TotalValue.Amount - convertedLastSnapshot.TotalInvested.Amount,
					GainLossPercentage = convertedLastSnapshot.TotalValue.Amount == 0 ? 0 : (convertedLastSnapshot.TotalValue.Amount - convertedLastSnapshot.TotalInvested.Amount) / convertedLastSnapshot.TotalValue.Amount * 100,
					Name = h.Holding.Name ?? string.Empty,
					Quantity = convertedLastSnapshot.Quantity,
					Symbol = h.Holding.Symbol,
					Sector = string.Join(",", h.Holding.SectorWeights.Select(x => x.Name)),
					Weight = 0,
				});
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