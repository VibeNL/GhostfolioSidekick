using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class HoldingIdentifierMappingService : IHoldingIdentifierMappingService
	{
		private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;
		private readonly ILogger<HoldingIdentifierMappingService> _logger;

		public HoldingIdentifierMappingService(
			IDbContextFactory<DatabaseContext> dbContextFactory,
			ILogger<HoldingIdentifierMappingService> logger)
		{
			_dbContextFactory = dbContextFactory;
			_logger = logger;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "Service needs to log and rethrow")]
		public async Task<HoldingIdentifierMappingModel?> GetHoldingIdentifierMappingAsync(string symbol, CancellationToken cancellationToken = default)
		{
			try
			{
				using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

				var holding = await context.Holdings
				  .Include(h => h.SymbolProfiles)
				   .FirstOrDefaultAsync(h => h.SymbolProfiles.Any(sp => sp.Symbol == symbol), cancellationToken);

				if (holding == null)
				{
					return null;
				}

				// Instead of using only the first symbol profile, use all symbol profiles
				var symbolProfiles = holding.SymbolProfiles;
				if (symbolProfiles == null || symbolProfiles.Count ==0)
				{
					return null;
				}

				var mappingModel = new HoldingIdentifierMappingModel
				{
					// Use the first symbol for backward compatibility, but include all profiles in DataProviderMappings
					Symbol = symbolProfiles[0].Symbol,
					Name = symbolProfiles[0].Name ?? string.Empty,
					HoldingId = holding.Id
				};

				// Map partial identifiers (from the JSON column)
				mappingModel.PartialIdentifiers = [.. holding.PartialSymbolIdentifiers
							.Select(pi => new PartialIdentifierDisplayModel
							{
								Identifier = pi.Identifier,
								AllowedAssetClasses = pi.AllowedAssetClasses,
								AllowedAssetSubClasses = pi.AllowedAssetSubClasses,
								MatchedDataProviders = [.. symbolProfiles
									.Where(sp => ContainsIdentifier(sp, pi.Identifier))
									.Select(sp => sp.DataSource)],
										HasUnresolvedMapping = !symbolProfiles.Any(sp => ContainsIdentifier(sp, pi.Identifier))
							})];

				// Map data provider mappings
				mappingModel.DataProviderMappings = [.. symbolProfiles
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
				  })];

				return mappingModel;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving holding identifier mapping for symbol: {Symbol}", symbol);
				throw;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "Service needs to log and rethrow")]
		public async Task<List<HoldingIdentifierMappingModel>> GetAllHoldingIdentifierMappingsAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

				var holdings = await context.Holdings
					.Include(h => h.SymbolProfiles)
					.Where(h => h.SymbolProfiles.Any()) // Only holdings with symbol profiles
					.ToListAsync(cancellationToken);

				var mappingModels = new List<HoldingIdentifierMappingModel>();

				foreach (var holding in holdings)
				{
					var symbolProfiles = holding.SymbolProfiles;
					if (symbolProfiles == null || symbolProfiles.Count == 0)
					{
						continue;
					}

					var mappingModel = new HoldingIdentifierMappingModel
					{
						// Use the first symbol for backward compatibility, but include all profiles in DataProviderMappings
						Symbol = symbolProfiles[0].Symbol,
						Name = symbolProfiles[0].Name ?? string.Empty,
						HoldingId = holding.Id
					};

					// Map partial identifiers (from the JSON column)
					mappingModel.PartialIdentifiers = [.. holding.PartialSymbolIdentifiers
					 .Select(pi => new PartialIdentifierDisplayModel
					 {
						 Identifier = pi.Identifier,
						 AllowedAssetClasses = pi.AllowedAssetClasses,
						 AllowedAssetSubClasses = pi.AllowedAssetSubClasses,
						 MatchedDataProviders = [.. symbolProfiles
								.Where(sp => ContainsIdentifier(sp, pi.Identifier))
								.Select(sp => sp.DataSource)],
						 HasUnresolvedMapping = !symbolProfiles.Any(sp => ContainsIdentifier(sp, pi.Identifier))
					 })];

					// Map data provider mappings
					mappingModel.DataProviderMappings = [.. symbolProfiles
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
					})];

					mappingModels.Add(mappingModel);
				}

				return [.. mappingModels.OrderBy(m => m.Symbol)];
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving all holding identifier mappings");
				throw;
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "Service needs to log and rethrow")]
		public async Task<List<IdentifierMatchingHistoryModel>> GetIdentifierMatchingHistoryAsync(string partialIdentifier, CancellationToken cancellationToken = default)
		{
			try
			{
				using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

				// For now, create a simple history based on current mappings
				// In a real implementation, you might have a separate audit/history table
				var holdings = await context.Holdings
								.Include(h => h.SymbolProfiles)
								.Where(h => h.PartialSymbolIdentifiers.Any(pi => pi.Identifier == partialIdentifier))
								.ToListAsync(cancellationToken);

				var historyModels = new List<IdentifierMatchingHistoryModel>();

				foreach (var holding in holdings)
				{
					foreach (var symbolProfile in holding.SymbolProfiles)
					{
						if (ContainsIdentifier(symbolProfile, partialIdentifier))
						{
							historyModels.Add(new IdentifierMatchingHistoryModel
							{
								PartialIdentifier = partialIdentifier,
								DataSource = symbolProfile.DataSource,
								MatchedSymbol = symbolProfile.Symbol,
							});
						}
					}
				}

				return [.. historyModels.OrderByDescending(h => h.PartialIdentifier)];
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving identifier matching history for: {Identifier}", partialIdentifier);
				throw;
			}
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