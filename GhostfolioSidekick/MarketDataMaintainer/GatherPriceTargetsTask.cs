using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.ExternalDataProvider.TipRanks;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class GatherPriceTargetsTask(
		IDbContextFactory<DatabaseContext> databaseContextFactory,
		ITargetPriceRepository targetPriceRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.MarketDataPriceTargets;

		public TimeSpan ExecutionFrequency => Frequencies.Hourly;

		public bool ExceptionsAreFatal => false;

		public string Name => "Gather Price Targets Task";

		public TimeSpan? MaxRunTime => null;

		public async Task DoWork(ILogger logger, CancellationToken cancellationToken)
		{
			await using var symbols = await databaseContextFactory.CreateDbContextAsync(cancellationToken);

			foreach (var symbol in symbols.SymbolProfiles.Where(x => x.DataSource == Datasource.TIPRANKS))
			{
				try
				{
					await GatherPriceTargetForSymbol(symbol, cancellationToken);
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Failed to gather price targets for {Symbol}", symbol.Symbol);
				}
			}
		}

		private async Task GatherPriceTargetForSymbol(SymbolProfile symbol, CancellationToken cancellationToken)
		{
			var priceTarget = await targetPriceRepository.GetPriceTarget(symbol);

			if (priceTarget == null)
			{
				return;
			}

			priceTarget.Symbol = symbol.Symbol;

			await ClearPriceTargetsAsync(symbol.Symbol, cancellationToken);

			using var db = await databaseContextFactory.CreateDbContextAsync(cancellationToken);
			db.PriceTargets.Add(priceTarget);
			await db.SaveChangesAsync(cancellationToken);
		}

		private async Task ClearPriceTargetsAsync(string symbol, CancellationToken cancellationToken)
		{
			await using var db = await databaseContextFactory.CreateDbContextAsync(cancellationToken);

			var existing = await db.PriceTargets
				.Where(x => x.Symbol == symbol)
				.ToListAsync(cancellationToken);

			db.PriceTargets.RemoveRange(existing);
			await db.SaveChangesAsync(cancellationToken);
		}
	}
}
