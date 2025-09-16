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
		ICurrencyExchange currencyExchange,
		ILogger<HoldingsDataServiceOLD> logger) : IHoldingsDataService
	{

		public Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, CancellationToken cancellationToken = default)
		{
			return GetHoldingsInternallyAsync(targetCurrency, null, cancellationToken);

		}

		public Task<List<HoldingDisplayModel>> GetHoldingsAsync(Currency targetCurrency, int accountId, CancellationToken cancellationToken = default)
		{
			return GetHoldingsInternallyAsync(targetCurrency, accountId == 0 ? null : accountId, cancellationToken);
		}

		public async Task<List<HoldingPriceHistoryPoint>> GetHoldingPriceHistoryAsync(
			string symbol,
			DateOnly startDate,
			DateOnly endDate,
			CancellationToken cancellationToken = default)
		{
			var snapShots = await databaseContext.CalculatedSnapshots
				.Where(x => x.Date >= startDate &&
							x.Date <= endDate)
				.GroupBy(x => x.Date)
				.Select(g => new HoldingPriceHistoryPoint
				{
					Date = g.Key,
					Price = g.Min(x => x.CurrentUnitPrice),
					AveragePrice = new Money(g.First().AverageCostPrice.Currency, g.Sum(x => x.AverageCostPrice.Amount * x.Quantity) / g.Sum(x => x.Quantity)),
				})
				.OrderBy(x => x.Date)
				.ToListAsync(cancellationToken);
			
			return snapShots;
		}

		public async Task<List<PortfolioValueHistoryPoint>> GetPortfolioValueHistoryAsync(
			Currency targetCurrency,
			DateOnly startDate, 
			DateOnly endDate, 
			int? accountId, 
			CancellationToken cancellationToken = default)
		{
			var snapShots = await databaseContext.CalculatedSnapshots
				.Where(x => (accountId == 0 || x.AccountId == accountId) && 
							x.Date >= startDate && 
							x.Date <= endDate)
				.GroupBy(x => x.Date)
				.Select(g => new PortfolioValueHistoryPoint
				{
					Date = g.Key,
					Value = g.Select(x => x.TotalValue).ToArray(),
					Invested = g.Select(x => x.TotalInvested).ToArray()
				})
				.OrderBy(x => x.Date)
				.ToListAsync(cancellationToken);

			// Convert values to target currency
			foreach (var point in snapShots)
			{
				point.Value = await Task.WhenAll(point.Value.Select(x => ConvertMoney(x, targetCurrency, point.Date)));
				point.Invested = await Task.WhenAll(point.Invested.Select(x => ConvertMoney(x, targetCurrency, point.Date)));
			}

			return snapShots;
		}

		private async Task<List<HoldingDisplayModel>> GetHoldingsInternallyAsync(Currency targetCurrency, int? accountId, CancellationToken cancellationToken)
		{
			if (accountId == null)
			{
				logger.LogDebug("Loading all holdings for portfolio in target currency {TargetCurrency}", targetCurrency);
			}
			else
			{
				logger.LogDebug("Loading holdings for account {AccountId} in target currency {TargetCurrency}", accountId, targetCurrency);
			}

			var list = await databaseContext.HoldingAggregateds
				.Select(x => new { Holding = x, LastSnapshot = x.CalculatedSnapshots.Where(x => x.AccountId == accountId || accountId == null).OrderByDescending(x => x.Date).FirstOrDefault() })
				.OrderBy(x => x.Holding.Symbol)
				.ToListAsync(cancellationToken);

			var result = new List<HoldingDisplayModel>();
			foreach (var x in list)
			{
				result.Add(new HoldingDisplayModel
				{
					AssetClass = x.Holding.AssetClass.ToString(),
					AveragePrice = await ConvertMoney(x.LastSnapshot?.AverageCostPrice, targetCurrency, x.LastSnapshot?.Date),
					Currency = targetCurrency.Symbol,
					CurrentPrice = await ConvertMoney(x.LastSnapshot?.CurrentUnitPrice, targetCurrency, x.LastSnapshot?.Date),
					CurrentValue = await ConvertMoney(x.LastSnapshot?.TotalValue, targetCurrency, x.LastSnapshot?.Date),
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
					x.GainLoss = new Money(targetCurrency, gainloss);

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

		private async Task<Money> ConvertMoney(Money? money, Currency targetCurrency, DateOnly? date)
		{
			if (money == null || date == null)
			{
				return new Money(targetCurrency, 0);
			}

			return await currencyExchange.ConvertMoney(money, targetCurrency, date.GetValueOrDefault());
		}
	}
}