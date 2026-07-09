using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.ExternalDataProvider.Yahoo
{
	public class PriceTargetRepository(DatabaseContext databaseContext) : IPriceTargetRepository
	{
		public async Task SavePriceTargetsAsync(IEnumerable<PriceTarget> priceTargets, string symbol)
		{
			await ClearPriceTargetsAsync(symbol);

			foreach (var priceTarget in priceTargets)
			{
				databaseContext.PriceTargets.Add(priceTarget);
			}

			await databaseContext.SaveChangesAsync();
		}

		public async Task ClearPriceTargetsAsync(string symbol)
		{
			var existing = await databaseContext.PriceTargets
				.Where(x => x.Symbol == symbol)
				.ToListAsync();

			databaseContext.PriceTargets.RemoveRange(existing);
			await databaseContext.SaveChangesAsync();
		}

		public async Task<IEnumerable<PriceTarget>> GetPriceTargetsAsync(SymbolProfile symbol)
		{
			return await databaseContext.PriceTargets
				.Where(x => x.Symbol == symbol.Symbol)
				.ToListAsync();
		}
	}
}
