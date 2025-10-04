using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class ServerConfigurationService(IApplicationSettings configuration) : IServerConfigurationService
	{
		public Currency PrimaryCurrency
		{
			get
			{
				var primaryCurrency = configuration.ConfigurationInstance?.Settings?.PrimaryCurrency;

				if (!string.IsNullOrWhiteSpace(primaryCurrency))
				{
					return Currency.GetCurrency(primaryCurrency);
				}

				return Currency.EUR;
			}
		}
	}
}
