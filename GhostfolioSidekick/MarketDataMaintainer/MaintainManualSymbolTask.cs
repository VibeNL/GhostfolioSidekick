using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI.API;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class MaintainManualSymbolTask(
		ILogger<MaintainManualSymbolTask> logger,
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IApplicationSettings applicationSettings) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.MaintainManualSymbol;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public async Task DoWork()
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();

			var symbolConfigurations = applicationSettings.ConfigurationInstance.Symbols;
			foreach (var symbolConfiguration in symbolConfigurations ?? [])
			{
				var manualSymbolConfiguration = symbolConfiguration.ManualSymbolConfiguration;
				if (manualSymbolConfiguration == null)
				{
					continue;
				}

				await AddAndUpdateSymbol(databaseContext, symbolConfiguration, manualSymbolConfiguration);
			}

			await databaseContext.SaveChangesAsync();
		}

		private async Task AddAndUpdateSymbol(DatabaseContext databaseContext, SymbolConfiguration symbolConfiguration, ManualSymbolConfiguration manualSymbolConfiguration)
		{
			var symbol = await databaseContext.SymbolProfiles
				.Include(x => x.MarketData)
				.Where(x => x.Symbol == symbolConfiguration.Symbol && x.DataSource == Datasource.MANUAL)
				.SingleOrDefaultAsync();
			if (symbol == null)
			{
				symbol = new SymbolProfile
				{
					Symbol = symbolConfiguration.Symbol,
					DataSource = Datasource.MANUAL,
				};
				databaseContext.SymbolProfiles.Add(symbol);
                logger.LogInformation("Added symbol {Symbol}.", symbolConfiguration.Symbol);
			}

			symbol.Name = manualSymbolConfiguration.Name;
			symbol.AssetClass = EnumMapper.ParseAssetClass(manualSymbolConfiguration.AssetClass);
			symbol.AssetSubClass = EnumMapper.ParseAssetSubClass(manualSymbolConfiguration.AssetSubClass);
			symbol.ISIN = manualSymbolConfiguration.ISIN;
			symbol.Currency = Currency.GetCurrency(manualSymbolConfiguration.Currency);
			symbol.Identifiers = [symbol.Name, symbol.ISIN];
			symbol.CountryWeight = manualSymbolConfiguration.Countries.Select(x => new CountryWeight
			{
				Name = x.Name,
				Weight = x.Weight,
				Code = x.Code,
				Continent = x.Continent
			}).ToArray();
			symbol.SectorWeights = manualSymbolConfiguration.Sectors.Select(x => new SectorWeight
			{
				Name = x.Name,
				Weight = x.Weight,
			}).ToArray();
            logger.LogDebug("Updated symbol {Symbol}.", symbolConfiguration.Symbol);
		}
	}
}
