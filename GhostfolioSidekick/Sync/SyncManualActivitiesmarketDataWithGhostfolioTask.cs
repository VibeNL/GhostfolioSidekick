﻿using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Sync
{
	internal class SyncManualActivitiesmarketDataWithGhostfolioTask(
			IDbContextFactory<DatabaseContext> databaseContextFactory,
			IGhostfolioSync ghostfolioSync) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.SyncManualActivitiesmarketDataWithGhostfolio;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public async Task DoWork()
		{
			await using var databaseContext = databaseContextFactory.CreateDbContext();
			var manualSymbolProfiles = await databaseContext.SymbolProfiles
				.Include(x => x.MarketData)
				.Where(x => x.DataSource == Datasource.MANUAL).ToListAsync();
			await ghostfolioSync.SyncSymbolProfiles(manualSymbolProfiles);

			foreach (var profile in manualSymbolProfiles)
			{
				var list = profile.MarketData.ToList();
				await ghostfolioSync.SyncMarketData(profile, list);
			}
		}
	}
}
