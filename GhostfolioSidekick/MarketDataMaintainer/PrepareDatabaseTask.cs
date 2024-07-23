using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Model;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class PrepareDatabaseTask(IMarketDataService marketDataService) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.PrepareDatabaseTask;

		public TimeSpan ExecutionFrequency => TimeSpan.FromDays(1);

		public async Task DoWork()
		{
			var dbContext = await DatabaseContext.GetDatabaseContext();

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
					});
				}
				else
				{
					profile.Name = record.Name;
					profile.Currency = currency;
					profile.DataSource = record.DataSource;
				}
			}

			await dbContext.SaveChangesAsync();

			foreach (var symbol in dbContext.SymbolProfiles.ToList().Where(x => !marketData.Any(s => s.Symbol == x.Symbol)))
			{
				dbContext.SymbolProfiles.Remove(symbol);
			}

			await dbContext.SaveChangesAsync();
		}
	}
}
