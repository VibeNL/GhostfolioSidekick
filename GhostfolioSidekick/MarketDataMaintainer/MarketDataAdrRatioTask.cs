using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider.Citi;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.MarketDataMaintainer;

/// <summary>
/// Periodically gathers ADR/GDR ratio (SharesPerReceipt) for all stock symbol profiles
/// that have a non-null ISIN, using the <see cref="IAdrRatioProvider"/> (Citi depositary
/// receipt lookup).  Follows the same pattern as <see cref="MarketDataStockSplitTask"/>.
/// </summary>
internal sealed class MarketDataAdrRatioTask(
	IDbContextFactory<DatabaseContext> databaseContextFactory,
	IAdrRatioProvider adrRatioProvider) : IScheduledWork
{
	public TaskPriority Priority => TaskPriority.MarketDataAdrRatio;

	public TimeSpan ExecutionFrequency => Frequencies.Hourly;

	public bool ExceptionsAreFatal => false;

	public string Name => "Market Data ADR/GDR Ratio Gatherer";

	public TimeSpan? MaxRunTime => null;

	public async Task DoWork(ILogger logger, CancellationToken cancellationToken)
	{
		var symbolIdentifiers = new List<Tuple<string, string>>();
		using (var databaseContext = await databaseContextFactory.CreateDbContextAsync(cancellationToken))
		{
			symbolIdentifiers.AddRange(
				(await databaseContext.SymbolProfiles
					.Where(x => x.AssetSubClass == AssetSubClass.Stock
						&& !string.IsNullOrWhiteSpace(x.ISIN))
					.Select(x => new Tuple<string, string>(x.Symbol, x.DataSource))
					.ToListAsync(cancellationToken))
					.OrderBy(x => x.Item1)
					.ThenBy(x => x.Item2)
					.Where(x => !Datasource.IsGhostfolio(x.Item2)));
		}

		foreach (var symbolIds in symbolIdentifiers)
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync(cancellationToken);
			var symbol = await databaseContext.SymbolProfiles
				.Where(x => x.Symbol == symbolIds.Item1 && x.DataSource == symbolIds.Item2)
				.SingleOrDefaultAsync(cancellationToken);

			if (symbol == null)
			{
				continue;
			}

			var ratio = await adrRatioProvider.GetSharesPerReceiptAsync(symbol.ISIN);
			var newRatio = ratio ?? 1m;

			if (symbol.SharesPerReceipt == newRatio)
			{
				continue;
			}

			var oldRatio = symbol.SharesPerReceipt;
			symbol.SharesPerReceipt = newRatio;

			await databaseContext.SaveChangesAsync(cancellationToken);
			logger.LogDebug(
				"Updated ADR/GDR ratio for {Symbol} from {DataSource}: {OldRatio} -> {NewRatio}",
				symbol.Symbol, symbol.DataSource, oldRatio, newRatio);
		}
	}
}
