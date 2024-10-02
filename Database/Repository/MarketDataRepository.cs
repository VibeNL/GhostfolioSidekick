using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Database.Repository
{
	public class MarketDataRepository(DatabaseContext databaseContext) : IMarketDataRepository
	{
		public IEnumerable<SymbolProfile> GetSymbols()
		{
			throw new NotImplementedException();
		}

		public async Task StoreAll(IEnumerable<MarketData> data)
		{
			await databaseContext.SymbolProfiles.AddRangeAsync(activities);
			await databaseContext.SaveChangesAsync();
		}
	}
}
