using System.Net.Http.Json;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Clients
{
	public class PortfolioClient(HttpClient httpClient, DatabaseContext databaseContext)
	{
		public async Task<Portfolio?> GetPortfolio(CancellationToken cancellationToken = default)
		{
			try
			{
				await databaseContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
				var a = databaseContext.Platforms.Any();

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
