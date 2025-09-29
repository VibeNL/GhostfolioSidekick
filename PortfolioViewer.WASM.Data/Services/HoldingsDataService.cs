using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

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
					AveragePrice = Money.Sum(group.Select(y => y.AverageCostPrice.Times(y.Quantity))).SafeDivide(group.Sum(x => x.Quantity)),
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
					Value = g.Sum(x => x.TotalValue),
					Invested = g.Sum(x => x.TotalInvested)
				})
				.OrderBy(x => x.Date)
				.ToListAsync(cancellationToken);

			return snapShots;
		}

		public async Task<HoldingDisplayModel?> GetHoldingAsync(string symbol, CancellationToken cancellationToken)
		{
			var lastKnownDate = await databaseContext.CalculatedSnapshotPrimaryCurrencies
				.MaxAsync(x => (DateOnly?)x.Date, cancellationToken);

			var item = databaseContext.HoldingAggregateds
				.Where(x => x.Symbol == symbol)
				.Select(x => new { x, LastSnapshots = x.CalculatedSnapshotsPrimaryCurrency.Where(y => y.Date == lastKnownDate) })
				.AsNoTracking()
				.AsEnumerable()
				.Select(g => new HoldingDisplayModel
				{
					Symbol = g.x.Symbol,
					Name = g.x.Name ?? g.x.Symbol,
					AssetClass = g.x.AssetClass.ToString(),
					Sector = g.x.SectorWeights?.FirstOrDefault()?.ToString() ?? string.Empty,
					Quantity = g.LastSnapshots.Sum(y => y.Quantity),
					AveragePrice = ConvertToMoney(SafeDivide(g.LastSnapshots.Sum(y => y.AverageCostPrice * y.Quantity), g.LastSnapshots.Sum(x => x.Quantity))),
					CurrentPrice = ConvertToMoney(g.LastSnapshots.Min(y => y.CurrentUnitPrice)),
					CurrentValue = ConvertToMoney(g.LastSnapshots.Sum(y => y.TotalValue)),
					Currency = serverConfigurationService.PrimaryCurrency.ToString(),
					GainLoss = ConvertToMoney(0),
					GainLossPercentage = 0,
					Weight = 0
				})
				.FirstOrDefault();

			return item;
		}

		private static decimal SafeDivide(decimal a, decimal b)
		{
			if (b == 0) return 0;
			return a / b;
		}

		private async Task<List<HoldingDisplayModel>> GetHoldingsInternallyAsync(int? accountId, CancellationToken cancellationToken)
		{
			var lastKnownDate = await databaseContext.CalculatedSnapshotPrimaryCurrencies
				.Where(x => accountId == null || x.AccountId == accountId)
				.MaxAsync(x => (DateOnly?)x.Date, cancellationToken);

			var list = await databaseContext.HoldingAggregateds
				.Where(x => x.CalculatedSnapshotsPrimaryCurrency.Any(y => y.AccountId == accountId || accountId == null))
				.Select(x => new { Holding = x, Snapshots = x.CalculatedSnapshotsPrimaryCurrency
					.Where(x => x.AccountId == accountId || accountId == null)
					.Where(x => x.Date == lastKnownDate) })
				.OrderBy(x => x.Holding.Symbol)
				.ToListAsync(cancellationToken);

			var result = new List<HoldingDisplayModel>();
			foreach (var x in list)
			{
				result.Add(new HoldingDisplayModel
				{
					AssetClass = x.Holding.AssetClass.ToString(),
					AveragePrice = ConvertToMoney(SafeDivide(x.Snapshots.Sum(y => y.AverageCostPrice * y.Quantity), x.Snapshots.Sum(x => x.Quantity))),
					Currency = serverConfigurationService.PrimaryCurrency.Symbol,
					CurrentPrice = ConvertToMoney(x.Snapshots.Min(y => y.CurrentUnitPrice)),
					CurrentValue = ConvertToMoney(x.Snapshots.Sum(y => y.TotalValue)),
					Name = x.Holding.Name ?? x.Holding.Symbol,
					Quantity = x.Snapshots.Sum(y => y.Quantity),
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