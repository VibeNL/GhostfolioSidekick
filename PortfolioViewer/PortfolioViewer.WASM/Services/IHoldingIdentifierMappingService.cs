using GhostfolioSidekick.PortfolioViewer.WASM.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public interface IHoldingIdentifierMappingService
	{
		/// <summary>
		/// Gets the identifier mapping information for a specific holding by symbol
		/// </summary>
		/// <param name="symbol">The holding symbol</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>The holding identifier mapping model or null if not found</returns>
		Task<HoldingIdentifierMappingModel?> GetHoldingIdentifierMappingAsync(string symbol, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all holdings with their identifier mappings
		/// </summary>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>List of all holding identifier mappings</returns>
		Task<List<HoldingIdentifierMappingModel>> GetAllHoldingIdentifierMappingsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the matching history for a specific partial identifier
		/// </summary>
		/// <param name="partialIdentifier">The partial identifier</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>List of matching history records</returns>
		Task<List<IdentifierMatchingHistoryModel>> GetIdentifierMatchingHistoryAsync(string partialIdentifier, CancellationToken cancellationToken = default);
	}
}