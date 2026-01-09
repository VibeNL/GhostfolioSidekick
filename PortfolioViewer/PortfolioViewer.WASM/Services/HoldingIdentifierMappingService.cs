using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class HoldingIdentifierMappingService(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		ILogger<HoldingIdentifierMappingService> logger) : IHoldingIdentifierMappingService
	{
		public async Task<HoldingIdentifierMappingModel?> GetHoldingIdentifierMappingAsync(string symbol, CancellationToken cancellationToken = default)
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

				var holding = await context.Holdings
				 .Include(h => h.SymbolProfiles)
				 .FirstOrDefaultAsync(h => h.SymbolProfiles.Any(sp => sp.Symbol == symbol), cancellationToken);

				if (holding == null)
				{
					return null;
				}

				var symbolProfiles = holding.SymbolProfiles;
				if (symbolProfiles == null || symbolProfiles.Count == 0)
				{
					return null;
				}

				return MapToHoldingIdentifierMappingModel(holding);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error retrieving holding identifier mapping for symbol: {Symbol}", symbol);
				throw;
			}
		}

		public async Task<List<HoldingIdentifierMappingModel>> GetAllHoldingIdentifierMappingsAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

				var holdings = await context.Holdings
					.Include(h => h.SymbolProfiles)
					.Where(h => h.SymbolProfiles.Any()) // Only holdings with symbol profiles
					.ToListAsync(cancellationToken);

				var mappingModels = holdings
					.Where(h => h.SymbolProfiles != null && h.SymbolProfiles.Count > 0)
					.Select(MapToHoldingIdentifierMappingModel)
					.OrderBy(m => m.Symbol)
					.ToList();

				return mappingModels;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error retrieving all holding identifier mappings");
				throw;
			}
		}

		public async Task<List<IdentifierMatchingHistoryModel>> GetIdentifierMatchingHistoryAsync(string partialIdentifier, CancellationToken cancellationToken = default)
		{
			try
			{
				using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

				// For now, create a simple history based on current mappings
				// In a real implementation, you might have a separate audit/history table
				var holdings = await context.Holdings
								.Include(h => h.SymbolProfiles)
								.Where(h => h.PartialSymbolIdentifiers.Any(pi => pi.Identifier == partialIdentifier))
								.ToListAsync(cancellationToken);

				var historyModels = new List<IdentifierMatchingHistoryModel>();

				foreach (var holding in holdings)
				{
					foreach (var symbolProfile in holding.SymbolProfiles.Where(x => ContainsIdentifier(x, partialIdentifier)))
					{
						historyModels.Add(new IdentifierMatchingHistoryModel
						{
							PartialIdentifier = partialIdentifier,
							DataSource = symbolProfile.DataSource,
							MatchedSymbol = symbolProfile.Symbol,
						});
					}
				}

				return [.. historyModels.OrderByDescending(h => h.PartialIdentifier)];
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error retrieving identifier matching history for: {Identifier}", partialIdentifier);
				throw;
			}
		}

		private static HoldingIdentifierMappingModel MapToHoldingIdentifierMappingModel(Model.Holding holding)
		{
			var symbolProfiles = holding.SymbolProfiles;
			var mappingModel = new HoldingIdentifierMappingModel
			{
				Symbol = symbolProfiles[0].Symbol,
				Name = symbolProfiles[0].Name ?? string.Empty,
				HoldingId = holding.Id,
				PartialIdentifiers = [.. holding.PartialSymbolIdentifiers
					.Select(pi => new PartialIdentifierDisplayModel
					{
						Identifier = pi.Identifier,
						AllowedAssetClasses = pi.AllowedAssetClasses,
						AllowedAssetSubClasses = pi.AllowedAssetSubClasses,
						MatchedDataProviders = [.. symbolProfiles
							.Where(sp => ContainsIdentifier(sp, pi.Identifier))
							.Select(sp => sp.DataSource)]
					})],
				DataProviderMappings = [.. symbolProfiles
					.Select(sp => new DataProviderMappingModel
					{
						DataSource = sp.DataSource,
						Symbol = sp.Symbol,
						Name = sp.Name ?? string.Empty,
						ISIN = sp.ISIN,
						AssetClass = sp.AssetClass,
						AssetSubClass = sp.AssetSubClass,
						Currency = sp.Currency.Symbol,
						Identifiers = sp.Identifiers,
						MatchedPartialIdentifiers = [.. holding.PartialSymbolIdentifiers
							.Where(pi => ContainsIdentifier(sp, pi.Identifier))
							.Select(pi => pi.Identifier)],
					})]
			};
			return mappingModel;
		}

		private static bool ContainsIdentifier(Model.Symbols.SymbolProfile symbolProfile, string identifier)
		{
			return symbolProfile.Identifiers.Any(id =>
				string.Equals(id, identifier, StringComparison.OrdinalIgnoreCase)) ||
				string.Equals(symbolProfile.Symbol, identifier, StringComparison.OrdinalIgnoreCase) ||
				(!string.IsNullOrEmpty(symbolProfile.ISIN) && string.Equals(symbolProfile.ISIN, identifier, StringComparison.OrdinalIgnoreCase));
		}
	}
}
