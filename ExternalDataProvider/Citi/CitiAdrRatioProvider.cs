using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.ExternalDataProvider.Citi
{
	/// <summary>
	/// Free data source for ADR/GDR shares-per-receipt ratios. Queries Citi's public Depositary Receipts
	/// "DR Program Information" widget by CUSIP (derived from a US-issued ISIN) and parses the
	/// "Ratio (ORD:DRS)" field directly from the response.
	/// </summary>
	public class CitiAdrRatioProvider(
		ILogger<CitiAdrRatioProvider> logger,
		HttpClient httpClient) : IAdrRatioProvider
	{
		public async Task<decimal?> GetSharesPerReceiptAsync(string? isin)
		{
			var cusip = CitiAdrRatioParser.TryGetCusipFromIsin(isin);
			if (cusip == null)
			{
				return null;
			}

			try
			{
				var url = $"https://depositaryreceipts.citi.com/adr/guides/pgm_d.aspx?pageid=15&subpageid=151&cusip={cusip}";
				using var response = await httpClient.GetAsync(url);

				if (!response.IsSuccessStatusCode)
				{
					logger.LogWarning("Failed to fetch Citi DR program information for CUSIP {Cusip}. Status Code: {StatusCode}", cusip, response.StatusCode);
					return null;
				}

				var content = await response.Content.ReadAsStringAsync();
				return CitiAdrRatioParser.TryParseSharesPerReceipt(content);
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
			{
				logger.LogWarning(ex, "Failed to retrieve ADR/GDR ratio from Citi for CUSIP {Cusip}", cusip);
				return null;
			}
		}
	}
}
