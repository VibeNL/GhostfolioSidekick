using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class MaintainManualSymbolTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		IApplicationSettings applicationSettings) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.MaintainManualSymbol;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Maintain Manual Symbol";

		public async Task DoWork(ILogger logger)
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

				await AddAndUpdateSymbol(logger, databaseContext, symbolConfiguration, manualSymbolConfiguration);
			}

			await databaseContext.SaveChangesAsync();
		}

		private static async Task AddAndUpdateSymbol(ILogger logger, DatabaseContext databaseContext, SymbolConfiguration symbolConfiguration, ManualSymbolConfiguration manualSymbolConfiguration)
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
			symbol.Identifiers = ListExtensions.FilterEmpty([symbol.Name, symbol.ISIN]);
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
