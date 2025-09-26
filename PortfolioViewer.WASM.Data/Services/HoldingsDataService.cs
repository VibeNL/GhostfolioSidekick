using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// Interface for portfolio data services. Implement this interface to provide real data to the Holdings page.
	/// </summary>
	public class HoldingsDataService(
		DatabaseContext databaseContext,
		IServerConfigurationService serverConfigurationService,
		ILogger<HoldingsDataService> logger) : IHoldingsDataService
	{

		public Task<List<HoldingDisplayModel>> GetHoldingsAsync(CancellationToken cancellationToken = default)
		{
			return GetHoldingsInternallyAsync(null, cancellationToken);

		}

		public Task<List<HoldingDisplayModel>> GetHoldingsAsync(int accountId, CancellationToken cancellationToken = default)
		{
			return GetHoldingsInternallyAsync(accountId == 0 ? null : accountId, cancellationToken);
		}

		public async Task<List<HoldingPriceHistoryPoint>> GetHoldingPriceHistoryAsync(
			string symbol,
			DateOnly startDate,
			DateOnly endDate,
			CancellationToken cancellationToken = default)
		{
			var rawSnapShots = await databaseContext.HoldingAggregateds
				.Where(x => x.Symbol == symbol)
				.SelectMany(x => x.CalculatedSnapshots)
				.Where(x => x.Date >= startDate &&
							x.Date <= endDate)
				.GroupBy(x => x.Date)
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			var snapShots = new List<HoldingPriceHistoryPoint>();
			foreach (var group in rawSnapShots)
			{
				snapShots.Add(new HoldingPriceHistoryPoint
				{
					Date = group.Key,
					Price = group.Min(x => x.CurrentUnitPrice),
					AveragePrice = Money.Sum(group.Select(x => x.AverageCostPrice.Times(x.Quantity))).SafeDivide(group.Sum(x => x.Quantity)),
				});
			}

			return snapShots;
		}

		public async Task<List<PortfolioValueHistoryPoint>> GetPortfolioValueHistoryAsync(
			DateOnly startDate,
			DateOnly endDate,
			int? accountId,
			CancellationToken cancellationToken = default)
		{
			var snapShots = await databaseContext.CalculatedSnapshotPrimaryCurrencies
				.Where(x => (accountId == 0 || x.AccountId == accountId) &&
							x.Date >= startDate &&
							x.Date <= endDate)
				.GroupBy(x => x.Date)
				.Select(g => new PortfolioValueHistoryPoint
				{
					Date = g.Key,
					Value = g.Select(x => new Money(serverConfigurationService.PrimaryCurrency, x.TotalValue)).ToArray(),
					Invested = g.Select(x => new Money(serverConfigurationService.PrimaryCurrency, x.TotalInvested)).ToArray()
				})
				.OrderBy(x => x.Date)
				.ToListAsync(cancellationToken);

			return snapShots;
		}

		private async Task<List<HoldingDisplayModel>> GetHoldingsInternallyAsync(int? accountId, CancellationToken cancellationToken)
		{
			var list = await databaseContext.HoldingAggregateds
				.Select(x => new { Holding = x, LastSnapshot = x.CalculatedSnapshotsPrimaryCurrency
					.Where(x => x.AccountId == accountId || accountId == null)
					.OrderByDescending(x => x.Date)
					.FirstOrDefault() })
				.OrderBy(x => x.Holding.Symbol)
				.ToListAsync(cancellationToken);

			var result = new List<HoldingDisplayModel>();
			foreach (var x in list)
			{
				result.Add(new HoldingDisplayModel
				{
					AssetClass = x.Holding.AssetClass.ToString(),
					AveragePrice = ConvertToMoney(x.LastSnapshot?.AverageCostPrice),
					Currency = serverConfigurationService.PrimaryCurrency.Symbol,
					CurrentPrice = ConvertToMoney(x.LastSnapshot?.CurrentUnitPrice),
					CurrentValue = ConvertToMoney(x.LastSnapshot?.TotalValue),
					Name = x.Holding.Name ?? x.Holding.Symbol,
					Quantity = x.LastSnapshot?.Quantity ?? 0,
					Sector = x.Holding.SectorWeights.Select(x => x.Name).FirstOrDefault()?.ToString() ?? string.Empty,
					Symbol = x.Holding.Symbol,
				});
			}

			// Calculate weights and gain/loss
			var totalValue = result.Sum(x => x.CurrentValue.Amount);
			if (totalValue > 0)
			{
				foreach (var x in result)
				{
					x.Weight = x.CurrentValue.Amount / totalValue;
					var gainloss = x.CurrentValue.Amount - (x.AveragePrice.Amount * x.Quantity);
					x.GainLoss = new Money(serverConfigurationService.PrimaryCurrency, gainloss);

					if (x.AveragePrice.Amount * x.Quantity == 0)
					{
						x.GainLossPercentage = 0;
					}
					else
					{
						x.GainLossPercentage = gainloss / (x.AveragePrice.Amount * x.Quantity);
					}
				}
			}

			return result;
		}

		private Money ConvertToMoney(decimal? amount)
		{
			return new Money(serverConfigurationService.PrimaryCurrency, amount ?? 0);
		}
	}
}