using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

public class PriceTargetsService(
	IDbContextFactory<DatabaseContext> dbContextFactory,
	IHoldingsDataService holdingsDataService) : IPriceTargetsService
{
	public async Task<List<PriceTargetDisplayModel>> GetPriceTargetsAsync(CancellationToken cancellationToken = default)
	{
		using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var priceTargets = await db.PriceTargets
			.Where(x => x.AverageTargetPriceAmount > 0)
			.OrderBy(x => x.Symbol)
			.ToListAsync(cancellationToken);

		return priceTargets.Select(pt => new PriceTargetDisplayModel
		{
			Symbol = pt.Symbol,
			Name = pt.Symbol,
			HighestTargetAmount = pt.HighestTargetPriceAmount,
			HighestTargetCurrency = pt.HighestTargetCurrency?.Symbol ?? "USD",
			AverageTargetAmount = pt.AverageTargetPriceAmount,
			AverageTargetCurrency = pt.AverageTargetCurrency?.Symbol ?? "USD",
			LowestTargetAmount = pt.LowestTargetPriceAmount,
			LowestTargetCurrency = pt.LowestTargetCurrency?.Symbol ?? "USD",
			Rating = pt.Rating.ToString(),
			NumberOfBuys = pt.NumberOfBuys,
			NumberOfHolds = pt.NumberOfHolds,
			NumberOfSells = pt.NumberOfSells,
		}).ToList();
	}

	public async Task<PriceTargetDisplayModel?> GetPriceTargetForSymbolAsync(string symbol, CancellationToken cancellationToken = default)
	{
		using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var pt = await db.PriceTargets
			.FirstOrDefaultAsync(x => x.Symbol == symbol, cancellationToken);

		if (pt == null) return null;

		return new PriceTargetDisplayModel
		{
			Symbol = pt.Symbol,
			Name = pt.Symbol,
			HighestTargetAmount = pt.HighestTargetPriceAmount,
			HighestTargetCurrency = pt.HighestTargetCurrency?.Symbol ?? "USD",
			AverageTargetAmount = pt.AverageTargetPriceAmount,
			AverageTargetCurrency = pt.AverageTargetCurrency?.Symbol ?? "USD",
			LowestTargetAmount = pt.LowestTargetPriceAmount,
			LowestTargetCurrency = pt.LowestTargetCurrency?.Symbol ?? "USD",
			Rating = pt.Rating.ToString(),
			NumberOfBuys = pt.NumberOfBuys,
			NumberOfHolds = pt.NumberOfHolds,
			NumberOfSells = pt.NumberOfSells,
		};
	}

	public async Task<List<HoldingPriceTargetDisplayModel>> GetHoldingsPriceTargetsAsync(CancellationToken cancellationToken = default)
	{
		var holdings = await holdingsDataService.GetHoldingsAsync(cancellationToken);

		using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var priceTargets = await db.PriceTargets
			.Where(x => x.AverageTargetPriceAmount > 0)
			.ToListAsync(cancellationToken);

		var priceTargetsBySymbol = priceTargets
			.GroupBy(x => x.Symbol)
			.ToDictionary(g => g.Key, g => g.First());

		var result = new List<HoldingPriceTargetDisplayModel>();
		foreach (var holding in holdings)
		{
			var symbol = holding.Symbols.FirstOrDefault(s => priceTargetsBySymbol.ContainsKey(s));
			if (symbol == null || !priceTargetsBySymbol.TryGetValue(symbol, out var pt))
			{
				continue;
			}

			var currentPriceAmount = holding.CurrentPrice.Amount;
			var proximityPercentage = pt.AverageTargetPriceAmount == 0
				? 0
				: currentPriceAmount / pt.AverageTargetPriceAmount * 100;

			result.Add(new HoldingPriceTargetDisplayModel
			{
				Symbol = symbol,
				Name = holding.Name,
				Quantity = holding.Quantity,
				CurrentPrice = holding.CurrentPrice,
				HighestTargetAmount = pt.HighestTargetPriceAmount,
				HighestTargetCurrency = pt.HighestTargetCurrency?.Symbol ?? "USD",
				AverageTargetAmount = pt.AverageTargetPriceAmount,
				AverageTargetCurrency = pt.AverageTargetCurrency?.Symbol ?? "USD",
				LowestTargetAmount = pt.LowestTargetPriceAmount,
				LowestTargetCurrency = pt.LowestTargetCurrency?.Symbol ?? "USD",
				Rating = pt.Rating.ToString(),
				NumberOfBuys = pt.NumberOfBuys,
				NumberOfHolds = pt.NumberOfHolds,
				NumberOfSells = pt.NumberOfSells,
				ProximityPercentage = proximityPercentage,
			});
		}

		return [.. result.OrderByDescending(x => x.ProximityPercentage)];
	}
}
