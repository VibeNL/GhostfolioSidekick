namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	public interface IPrivacyModeService
	{
		bool IsPrivacyMode { get; }

		void Toggle();

		event Action? OnChange;
	}
}
