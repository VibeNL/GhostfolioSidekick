using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public interface IServerConfigurationService
	{
		Currency PrimaryCurrency { get; }
	}
}
