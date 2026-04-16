namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public class PrivacyModeService : IPrivacyModeService
    {
        public bool IsPrivacyMode { get; private set; }

        public event Action? OnChange;

        public void Toggle()
        {
            IsPrivacyMode = !IsPrivacyMode;
            OnChange?.Invoke();
        }
    }
}
