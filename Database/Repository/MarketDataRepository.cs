using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Database.Repository
{
	public class MarketDataRepository(DatabaseContext databaseContext) : IMarketDataRepository
	{
		public Task<SymbolProfile?> GetSymbolProfileBySymbol(string symbolString)
		{
			return databaseContext.SymbolProfiles.SingleOrDefaultAsync(x => x.Symbol == symbolString);
		}

		public async Task<IEnumerable<SymbolProfile>> GetSymbolProfiles()
		{
			return await databaseContext.SymbolProfiles.ToListAsync();
		}

		public async Task Store(SymbolProfile symbolProfile)
		{
			ArgumentNullException.ThrowIfNull(symbolProfile);

			if (!await databaseContext.SymbolProfiles.ContainsAsync(symbolProfile).ConfigureAwait(false))
			{
				await databaseContext.SymbolProfiles.AddAsync(symbolProfile);
			}

			await databaseContext.SaveChangesAsync();
		}
	}
}
