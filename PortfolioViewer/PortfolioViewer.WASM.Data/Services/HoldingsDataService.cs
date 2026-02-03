using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// Interface for portfolio data services. Implement this interface to provide real data to the Holdings page.
	/// </summary>
	public class HoldingsDataService(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		IServerConfigurationService serverConfigurationService) : IHoldingsDataService
	{

		public Task<List<HoldingDisplayModel>> GetHoldingsAsync(CancellationToken cancellationToken = default)
		{
			return GetHoldingsInternallyAsync(null, cancellationToken);
		}

		public Task<List<HoldingDisplayModel>> GetHoldingsAsync(int accountId, CancellationToken cancellationToken = default)
		{
			return GetHoldingsInternallyAsync(accountId == 0 ? null : accountId, cancellationToken);
		}

		public async Task<HoldingDisplayModel?> GetHoldingAsync(string symbol, CancellationToken cancellationToken = default)
		{
			// Get all holdings due to weight and gain/loss calculation
			var holdings = await GetHoldingsInternallyAsync(null, cancellationToken);
			return holdings.FirstOrDefault(x => x.Symbol == symbol);
		}


		public async Task<List<HoldingPriceHistoryPoint>> GetHoldingPriceHistoryAsync(
			string symbol,
			DateOnly startDate,
			DateOnly endDate,
			CancellationToken cancellationToken = default)
		{
			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var rawSnapShots = await databaseContext.Holdings
				.Where(x => x.SymbolProfiles.Any(sp => sp.Symbol == symbol))
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
					AveragePrice = group.Sum(y => y.AverageCostPrice * y.Quantity) / group.Sum(x => x.Quantity),
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
			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var snapShots = await databaseContext.CalculatedSnapshots
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

		private async Task<List<HoldingDisplayModel>> GetHoldingsInternallyAsync(int? accountId, CancellationToken cancellationToken)
		{
			using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var lastKnownDate = await databaseContext.CalculatedSnapshots
				.Where(x => accountId == null || x.AccountId == accountId)
				.MaxAsync(x => (DateOnly?)x.Date, cancellationToken);

			var list = await databaseContext.Holdings
				.Where(x => x.CalculatedSnapshots.Any(y => y.AccountId == accountId || accountId == null))
				.Select(x => new
				{
					Holding = x,
					Snapshots = x.CalculatedSnapshots
						.Where(x => x.AccountId == accountId || accountId == null)
						.Where(x => x.Date == lastKnownDate)
				})
				.OrderBy(x => x.Holding.Id)
				.ToListAsync(cancellationToken);

			var result = new List<HoldingDisplayModel>();
			foreach (var x in list)
			{
				// Skip holdings with no snapshots to avoid NoElements exceptions
				if (!x.Snapshots.Any() || x.Holding.SymbolProfiles.Count == 0)
				{
					continue;
				}

				var symbolProfile = x.Holding.SymbolProfiles[0];

				result.Add(new HoldingDisplayModel
				{
					AssetClass = symbolProfile.AssetClass.ToString() ?? "",
					AveragePrice = ConvertToMoney(SafeDivide(x.Snapshots.Sum(y => y.AverageCostPrice * y.Quantity), x.Snapshots.Sum(x => x.Quantity))),
					Currency = serverConfigurationService.PrimaryCurrency.Symbol,
					CurrentPrice = ConvertToMoney(x.Snapshots.Min(y => y.CurrentUnitPrice)),
					CurrentValue = ConvertToMoney(x.Snapshots.Sum(y => y.TotalValue)),
					Name = symbolProfile.Name ?? symbolProfile.Symbol,
					Quantity = x.Snapshots.Sum(y => y.Quantity),
					Sector = symbolProfile.SectorWeights.Select(x => x.Name).FirstOrDefault()?.ToString() ?? string.Empty,
					Symbol = symbolProfile.Symbol,
					GainLoss = Money.Zero(serverConfigurationService.PrimaryCurrency),
					GainLossPercentage = 0,
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

		return [.. result.OrderBy(x => x.Symbol)];
	}

		private Money ConvertToMoney(decimal? amount)
		{
			return new Money(serverConfigurationService.PrimaryCurrency, amount ?? 0);
		}

		private static decimal SafeDivide(decimal a, decimal b)
		{
			if (b == 0) return 0;
			return a / b;
		}
	}
}