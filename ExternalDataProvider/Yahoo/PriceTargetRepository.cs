using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.ExternalDataProvider.Yahoo
{
	public class PriceTargetRepository(DatabaseContext databaseContext) : IPriceTargetRepository
	{
		public async Task SavePriceTargetsAsync(IEnumerable<PriceTarget> priceTargets, string symbol, CancellationToken cancellationToken = default)
		{
			await ClearPriceTargetsAsync(symbol, cancellationToken);

			foreach (var priceTarget in priceTargets)
			{
				databaseContext.PriceTargets.Add(priceTarget);
			}

			await databaseContext.SaveChangesAsync(cancellationToken);
		}

		public async Task ClearPriceTargetsAsync(string symbol, CancellationToken cancellationToken = default)
		{
			var existing = await databaseContext.PriceTargets
				.Where(x => x.Symbol == symbol)
				.ToListAsync(cancellationToken);

			databaseContext.PriceTargets.RemoveRange(existing);
			await databaseContext.SaveChangesAsync(cancellationToken);
		}

		public async Task<IEnumerable<PriceTarget>> GetPriceTargetsAsync(SymbolProfile symbol, CancellationToken cancellationToken = default)
		{
			return await databaseContext.PriceTargets
				.Where(x => x.Symbol == symbol.Symbol)
				.ToListAsync(cancellationToken);
		}
	}
}
