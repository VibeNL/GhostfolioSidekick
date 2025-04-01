using static PortfolioViewer.WASM.Pages.Weather;
using System.Net.Http.Json;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model;

namespace PortfolioViewer.WASM.Clients
{
	public class PortfolioClient(HttpClient httpClient)
	{
		public async Task<Portfolio?> GetPortfolio(CancellationToken cancellationToken = default)
		{
			try
			{
				var portfolio = await httpClient.GetFromJsonAsync<Portfolio>("/profolio", cancellationToken);
				return portfolio;
			}
			catch (Exception ex)
			{
				return null;
			}
		}
	}
}
