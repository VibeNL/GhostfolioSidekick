using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

public class PriceTargetsService(
	IDbContextFactory<DatabaseContext> dbContextFactory) : IPriceTargetsService
{
	public async Task<List<PriceTargetDisplayModel>> GetPriceTargetsAsync(CancellationToken cancellationToken = default)
	{
		using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
		var priceTargets = await db.PriceTargets
			.Where(x => x.AverageTargetPriceAmount > 0)
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
		}).OrderBy(x => x.Symbol).ToList();
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
}
