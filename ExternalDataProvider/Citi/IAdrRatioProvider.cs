namespace GhostfolioSidekick.ExternalDataProvider.Citi
{
	/// <summary>
	/// Provides the number of underlying ordinary shares represented by one ADR/GDR unit (SharesPerReceipt),
	/// sourced from a free depositary-receipt program lookup (e.g. Citi's public DR Program Information page).
	/// </summary>
	public interface IAdrRatioProvider
	{
		/// <summary>
		/// Attempts to determine the ADR/GDR ratio (shares per receipt) for the given ISIN.
		/// Returns null if the ISIN is not a supported (US-issued) depositary receipt ISIN, or if the ratio
		/// could not be found/parsed.
		/// </summary>
		Task<decimal?> GetSharesPerReceiptAsync(string? isin);
	}
}
