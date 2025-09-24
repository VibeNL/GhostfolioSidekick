using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public interface IServerConfigurationService
	{
		Currency PrimaryCurrency { get; }
	}
}
