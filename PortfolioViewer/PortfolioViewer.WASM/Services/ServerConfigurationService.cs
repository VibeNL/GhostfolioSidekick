using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public class ServerConfigurationService : IServerConfigurationService
	{
		public Currency PrimaryCurrency => Currency.EUR; // TODO: Make configurable
	}
}
