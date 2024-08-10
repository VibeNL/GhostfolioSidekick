using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Model;
using GhostfolioSidekick.GhostfolioAPI;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.DatabaseMaintainer
{
	internal class SyncSymbolsWithGhostfolio(IMarketDataService marketDataService)
	{
		public async Task Sync()
		{
			using var dbContext = await DatabaseContext.GetDatabaseContext();
			var marketData = (await marketDataService.GetAllSymbolProfiles()).ToList();
			foreach (var record in marketData)
			{
				var currency = await dbContext.Currencies.FirstOrDefaultAsync(c => c.Symbol == record.Currency.Symbol);
				if (currency == null)
				{
					currency = new Currency
					{
						Symbol = record.Currency.Symbol
					};
					await dbContext.Currencies.AddAsync(currency);
					await dbContext.SaveChangesAsync();
				}

				var profile = await dbContext.SymbolProfiles.FirstOrDefaultAsync(s => s.Symbol == record.Symbol);
				if (profile == null)
				{
					await dbContext.SymbolProfiles.AddAsync(new Database.Model.SymbolProfile
					{
						Symbol = record.Symbol,
						Name = record.Name,
						Currency = currency,
						DataSource = record.DataSource,
						AssetClass = Enum.Parse<AssetClass>(record.AssetClass.ToString()),
						AssetSubClass = record.AssetSubClass != null ? Enum.Parse<AssetSubClass>(record.AssetSubClass!.ToString()!) : null
					});
				}
				else
				{
					profile.Name = record.Name;
					profile.Currency = currency;
					profile.DataSource = record.DataSource;
					profile.AssetClass = Enum.Parse<AssetClass>(record.AssetClass.ToString());
					profile.AssetSubClass = record.AssetSubClass != null ? Enum.Parse<AssetSubClass>(record.AssetSubClass!.ToString()!) : null;
				}
			}
			await dbContext.SaveChangesAsync();

			foreach (var symbol in (await dbContext.SymbolProfiles.ToListAsync()).Where(x => !marketData.Any(s => s.Symbol == x.Symbol)))
			{
				dbContext.SymbolProfiles.Remove(symbol);
			}

			await dbContext.SaveChangesAsync();
		}
	}
}
