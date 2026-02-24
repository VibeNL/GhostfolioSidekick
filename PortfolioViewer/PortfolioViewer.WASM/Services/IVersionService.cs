namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public interface IVersionService
	{
		string ClientVersion { get; }
		Task<string?> GetServerVersionAsync();
		Task<bool> IsUpdateAvailableAsync();
	}
}
